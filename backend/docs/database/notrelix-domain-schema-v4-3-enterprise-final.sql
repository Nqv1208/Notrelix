-- =============================================================================
-- Notrelix Domain Schema V4.3 — Enterprise Final Baseline
-- =============================================================================
--
-- Generated: 2026-06-20T00:00:00+07:00
--
-- Based on:
--   - notrelix-domain-schema-v2.sql
--
-- Senior architecture alignment:
--   - Domain bounded contexts are:
--       Identity, Workspaces, Governance, WorkManagement, Documents,
--       Collaboration, Automation, Integrations, Billing, Analytics optional.
--   - Search, Operations, Outbox, JobLocks, Idempotency, Import/Export Jobs,
--     ProcessedEvents and SearchIndexJobs are not core Domain bounded contexts.
--   - Database schemas search.* and ops.* remain for projection/technical storage.
--   - Soft delete is the default for business resources.
--   - Hard delete is blocked at DB level for business tables unless explicitly
--     enabled in a controlled purge transaction.
--
-- Table count:
--   - Logical domain tables     : 98
--   - Partition child tables    : 8
--   - Physical CREATE TABLEs    : 106
-- =============================================================================


-- SECTION 1: BASE DOMAIN SCHEMA V2
-- =============================================================================

-- =============================================================================
-- Notrelix Enterprise Service-Ready Multi-Schema Domain Schema
-- Version: 2.0 — Production-hardened
-- =============================================================================
--
-- Purpose:
--   Enterprise-grade PostgreSQL schema organized by bounded context ownership.
--   Designed for Modular Monolith now, service/database split later.
--
-- What changed in v2.0 vs v1:
--   + 56 updated_at triggers (all tables with updated_at now auto-maintained)
--   + 18 partial indexes WHERE deleted_at IS NULL (soft-delete perf fix)
--   + Row-Level Security (RLS) on all workspace-scoped tables (tenant isolation)
--   + tsvector column + GIN index on search.search_documents (full-text search)
--   + tsvector update trigger with weighted ranking (A=title, B=content, C=tags)
--   + PARTITION BY RANGE on collab.activity_logs (monthly partitions)
--   + PARTITION BY RANGE on governance.audit_logs (yearly partitions)
--   + TTL cleanup functions for sessions, idempotency keys, login attempts,
--     search jobs, outbox messages — seeded into automation.scheduled_jobs
--   + Cross-schema FK migration guide (section 18)
--   + ops.current_workspace_id() helper for RLS
--   + notrelix_app role for least-privilege DB access
--
-- Deployment model now:
--   One PostgreSQL database, multiple schemas by bounded context.
--
-- Future model:
--   Each schema can become a database owned by a separate service.
--   See section 18 for the cross-schema FK migration guide.
--
-- Bounded context schemas (12):
--   identity     -> users / sessions / OAuth / MFA / security tokens
--   workspace    -> workspaces / members / teams / invitations / spaces
--   governance   -> permissions / share links / audit / policy / custom roles
--   work         -> boards / fields / items / views / work management
--   docs         -> pages / blocks / versions / resource links
--   collab       -> comments / notifications / activity / attachments / presence
--   automation   -> rules / executions / scheduled jobs / outbox pattern
--   integration  -> external integrations / webhooks / calendar sync
--   billing      -> plans / subscriptions / invoices / entitlements / usage
--   reporting    -> dashboards / widgets / reporting projections
--   search       -> search documents (pg_trgm + tsvector) / index jobs
--   ops          -> idempotency / import-export / job locks / TTL cleanup
--
-- Total: 98 tables, 56 triggers, ~80 indexes, 12 schemas, 4 legacy views
--
-- Prerequisites:
--   PostgreSQL 15+ (for partitioning improvements and MERGE support)
--   Extensions: pgcrypto, citext, pg_trgm
--
-- Notes:
--   - UUID generation uses gen_random_uuid() (v4) at DB level.
--     For UUIDv7 (time-ordered), generate in application layer using
--     UUIDNext (.NET) or uuid7 (Node/Python) and drop these DEFAULT clauses.
--   - RLS uses SET LOCAL app.current_workspace_id = '<uuid>' per transaction.
--     The notrelix_app role must be granted on all schemas after deployment.
--   - Partition maintenance: extend partition tables quarterly via migration
--     or install pg_partman for automatic partition management.
--   - Do not let modules query each other's tables directly in application code.
--     Cross-context reads should go through projections or read models.
-- =============================================================================


CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS workspace;
CREATE SCHEMA IF NOT EXISTS governance;
CREATE SCHEMA IF NOT EXISTS work;
CREATE SCHEMA IF NOT EXISTS docs;
CREATE SCHEMA IF NOT EXISTS collab;
CREATE SCHEMA IF NOT EXISTS automation;
CREATE SCHEMA IF NOT EXISTS integration;
CREATE SCHEMA IF NOT EXISTS billing;
CREATE SCHEMA IF NOT EXISTS reporting;
CREATE SCHEMA IF NOT EXISTS search;
CREATE SCHEMA IF NOT EXISTS ops;


CREATE OR REPLACE FUNCTION ops.set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- =============================================================================
-- 01. identity schema
-- Service candidate: Identity Service
-- Owns: users, profiles, sessions, OAuth, MFA/security tokens
-- =============================================================================

CREATE TABLE IF NOT EXISTS identity.users (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email                 citext NOT NULL UNIQUE,
    normalized_email      citext NOT NULL UNIQUE,
    user_name             varchar(120),
    display_name          varchar(160),
    password_hash         text,
    avatar_url            text,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'PendingVerification', 'Suspended', 'Deleted')),
    email_verified_at     timestamptz,
    last_login_at         timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid,
    updated_at            timestamptz,
    updated_by            uuid,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE TRIGGER trg_identity_users_updated_at
BEFORE UPDATE ON identity.users
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_identity_users_status ON identity.users(status);
CREATE INDEX IF NOT EXISTS ix_identity_users_display_name_trgm ON identity.users USING gin(display_name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_identity_users_active ON identity.users(id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS identity.user_profiles (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL UNIQUE REFERENCES identity.users(id) ON DELETE CASCADE,
    bio                   text,
    phone_number          varchar(40),
    timezone              varchar(80),
    locale                varchar(20),
    preferences_json      jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);

CREATE TRIGGER trg_identity_user_profiles_updated_at
BEFORE UPDATE ON identity.user_profiles
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE TABLE IF NOT EXISTS identity.user_sessions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    refresh_token_hash    text NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Revoked', 'Expired')),
    ip_address            inet,
    user_agent            text,
    expires_at            timestamptz NOT NULL,
    revoked_at            timestamptz,
    revoked_reason        text,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_identity_user_sessions_updated_at
BEFORE UPDATE ON identity.user_sessions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_identity_user_sessions_user_status ON identity.user_sessions(user_id, status);
CREATE INDEX IF NOT EXISTS ix_identity_user_sessions_expires_at ON identity.user_sessions(expires_at);

CREATE TABLE IF NOT EXISTS identity.oauth_accounts (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    provider              varchar(40) NOT NULL CHECK (provider IN ('Google', 'GitHub', 'Microsoft', 'Apple')),
    provider_user_id      varchar(255) NOT NULL,
    provider_email        citext,
    access_token_ref      text,
    refresh_token_ref     text,
    expires_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_identity_oauth_provider_user UNIQUE(provider, provider_user_id),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_identity_oauth_accounts_updated_at
BEFORE UPDATE ON identity.oauth_accounts
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS identity.user_security_settings (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL UNIQUE REFERENCES identity.users(id) ON DELETE CASCADE,
    is_mfa_enabled        boolean NOT NULL DEFAULT false,
    preferred_mfa_method  varchar(40)
                          CHECK (preferred_mfa_method IS NULL OR preferred_mfa_method IN ('AuthenticatorApp', 'Email', 'Sms', 'RecoveryCode')),
    require_password_change boolean NOT NULL DEFAULT false,
    password_changed_at   timestamptz,
    last_security_review_at timestamptz,
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_identity_user_security_settings_updated_at
BEFORE UPDATE ON identity.user_security_settings
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS identity.user_mfa_methods (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    method_type           varchar(40) NOT NULL CHECK (method_type IN ('AuthenticatorApp', 'Email', 'Sms', 'RecoveryCode')),
    secret_ref            text,
    destination_masked    varchar(160),
    is_verified           boolean NOT NULL DEFAULT false,
    is_primary            boolean NOT NULL DEFAULT false,
    verified_at           timestamptz,
    disabled_at           timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_identity_user_mfa_methods_updated_at
BEFORE UPDATE ON identity.user_mfa_methods
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE UNIQUE INDEX IF NOT EXISTS ux_identity_user_mfa_primary
ON identity.user_mfa_methods(user_id)
WHERE is_primary = true AND disabled_at IS NULL;

CREATE TABLE IF NOT EXISTS identity.user_login_attempts (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    email                 citext,
    succeeded             boolean NOT NULL DEFAULT false,
    failure_reason        varchar(120),
    ip_address            inet,
    user_agent            text,
    occurred_at           timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_identity_user_login_attempts_email_time
ON identity.user_login_attempts(email, occurred_at DESC);

CREATE TABLE IF NOT EXISTS identity.email_verification_tokens (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash            text NOT NULL UNIQUE,
    email                 citext NOT NULL,
    expires_at            timestamptz NOT NULL,
    consumed_at           timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.password_reset_tokens (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash            text NOT NULL UNIQUE,
    expires_at            timestamptz NOT NULL,
    consumed_at           timestamptz,
    ip_address            inet,
    user_agent            text,
    created_at            timestamptz NOT NULL DEFAULT now()
);


-- =============================================================================
-- 02. workspace schema
-- Service candidate: Workspace Service
-- Owns: workspaces, members, teams, invitations, spaces
-- =============================================================================

CREATE TABLE IF NOT EXISTS workspace.workspaces (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_user_id         uuid NOT NULL REFERENCES identity.users(id),
    name                  varchar(160) NOT NULL,
    slug                  varchar(180) NOT NULL UNIQUE,
    description           text,
    logo_url              text,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    plan_code             varchar(80) DEFAULT 'free',
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);

CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_owner ON workspace.workspaces(owner_user_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_status ON workspace.workspaces(status);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_name_trgm ON workspace.workspaces USING gin(name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_active ON workspace.workspaces(owner_user_id) WHERE deleted_at IS NULL;


CREATE TRIGGER trg_workspace_workspaces_updated_at
BEFORE UPDATE ON workspace.workspaces
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE TABLE IF NOT EXISTS workspace.workspace_members (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role                  varchar(40) NOT NULL CHECK (role IN ('Owner', 'Admin', 'Member', 'Guest')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Suspended', 'Removed', 'Pending')),
    joined_at             timestamptz,
    invited_by_user_id    uuid REFERENCES identity.users(id),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_workspace_members_workspace_user UNIQUE(workspace_id, user_id),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_workspace_members_workspace_id_id UNIQUE(workspace_id, id)
);
CREATE TRIGGER trg_workspace_workspace_members_updated_at
BEFORE UPDATE ON workspace.workspace_members
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_workspace_members_user ON workspace.workspace_members(user_id);
CREATE INDEX IF NOT EXISTS ix_workspace_members_role ON workspace.workspace_members(workspace_id, role);
CREATE INDEX IF NOT EXISTS ix_workspace_members_status ON workspace.workspace_members(workspace_id, status);

CREATE TABLE IF NOT EXISTS workspace.workspace_invitations (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    email                 citext NOT NULL,
    role                  varchar(40) NOT NULL DEFAULT 'Member' CHECK (role IN ('Admin', 'Member', 'Guest')),
    token_hash            text NOT NULL UNIQUE,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Accepted', 'Revoked', 'Expired')),
    invited_by_user_id    uuid NOT NULL REFERENCES identity.users(id),
    accepted_by_user_id   uuid REFERENCES identity.users(id),
    accepted_at           timestamptz,
    revoked_at            timestamptz,
    expires_at            timestamptz NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_workspace_workspace_invitations_updated_at
BEFORE UPDATE ON workspace.workspace_invitations
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_workspace_invitations_workspace ON workspace.workspace_invitations(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_invitations_email ON workspace.workspace_invitations(email);

CREATE TABLE IF NOT EXISTS workspace.spaces (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    parent_space_id       uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    name                  varchar(160) NOT NULL,
    description           text,
    icon                  varchar(80),
    color                 varchar(20),
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    visibility            varchar(40) NOT NULL DEFAULT 'Workspace'
                          CHECK (visibility IN ('Private', 'Workspace')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    space_type varchar(40) NOT NULL DEFAULT 'Folder',
    CONSTRAINT ux_workspace_spaces_workspace_id_id UNIQUE(workspace_id, id)
);
CREATE TRIGGER trg_workspace_spaces_updated_at
BEFORE UPDATE ON workspace.spaces
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_workspace_spaces_workspace_position ON workspace.spaces(workspace_id, position);
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_parent ON workspace.spaces(parent_space_id);
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_active ON workspace.spaces(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS workspace.teams (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(160) NOT NULL,
    description           text,
    color                 varchar(20),
    avatar_url            text,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    CONSTRAINT ux_workspace_teams_name UNIQUE(workspace_id, name),
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE INDEX IF NOT EXISTS ix_workspace_teams_active ON workspace.teams(workspace_id) WHERE deleted_at IS NULL;

CREATE TRIGGER trg_workspace_teams_updated_at
BEFORE UPDATE ON workspace.teams
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS workspace.team_members (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    team_id               uuid NOT NULL REFERENCES workspace.teams(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role                  varchar(40) NOT NULL DEFAULT 'Member' CHECK (role IN ('Manager', 'Member')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_workspace_team_members_team_user UNIQUE(team_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_workspace_team_members_user ON workspace.team_members(workspace_id, user_id);


-- =============================================================================
-- 03. governance schema
-- Service candidate: Governance Service
-- Owns: permission, share link, audit, security, policy, custom roles
-- =============================================================================

CREATE TABLE IF NOT EXISTS governance.resource_permissions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    subject_type          varchar(80) NOT NULL CHECK (subject_type IN ('User', 'WorkspaceRole', 'Team', 'PublicLink', 'ExternalEmail')),
    subject_id            uuid,
    level                 varchar(40) NOT NULL CHECK (level IN ('None', 'Viewer', 'Commenter', 'Editor', 'Manager', 'Owner')),
    granted_by_user_id    uuid REFERENCES identity.users(id),
    granted_at            timestamptz NOT NULL DEFAULT now(),
    expires_at            timestamptz,
    is_revoked            boolean NOT NULL DEFAULT false,
    revoked_by_user_id    uuid REFERENCES identity.users(id),
    revoked_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_governance_resource_permissions_scope_subject
        UNIQUE(workspace_id, resource_type, resource_id, subject_type, subject_id),
    deleted_at timestamptz,
    deleted_by uuid,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid,
    version bigint NOT NULL DEFAULT 1,
    effect varchar(20) NOT NULL DEFAULT 'Allow',
    condition_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    priority integer NOT NULL DEFAULT 100,
    CONSTRAINT ck_governance_resource_permissions_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_governance_resource_permissions_updated_at
BEFORE UPDATE ON governance.resource_permissions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_lookup
ON governance.resource_permissions(workspace_id, resource_type, resource_id)
WHERE is_revoked = false;

CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_subject
ON governance.resource_permissions(workspace_id, subject_type, subject_id)
WHERE is_revoked = false;

CREATE TABLE IF NOT EXISTS governance.field_permissions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL,
    field_id              uuid NOT NULL,
    subject_type          varchar(80) NOT NULL CHECK (subject_type IN ('User', 'WorkspaceRole', 'Team')),
    subject_id            uuid,
    can_view              boolean NOT NULL DEFAULT true,
    can_edit              boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_governance_field_permissions_subject
        UNIQUE(workspace_id, board_id, field_id, subject_type, subject_id),
    version bigint NOT NULL DEFAULT 1,
    effect varchar(20) NOT NULL DEFAULT 'Allow',
    can_mask boolean NOT NULL DEFAULT false,
    condition_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE TRIGGER trg_governance_field_permissions_updated_at
BEFORE UPDATE ON governance.field_permissions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS governance.share_links (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    token_hash            text NOT NULL UNIQUE,
    level                 varchar(40) NOT NULL DEFAULT 'Viewer'
                          CHECK (level IN ('Viewer', 'Commenter', 'Editor')),
    status                varchar(40) NOT NULL DEFAULT 'Enabled'
                          CHECK (status IN ('Enabled', 'Disabled', 'Expired')),
    expires_at            timestamptz,
    created_by_user_id    uuid NOT NULL REFERENCES identity.users(id),
    disabled_by_user_id   uuid REFERENCES identity.users(id),
    disabled_at           timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    CONSTRAINT ck_governance_share_links_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    version bigint NOT NULL DEFAULT 1);
CREATE TRIGGER trg_governance_share_links_updated_at
BEFORE UPDATE ON governance.share_links
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_governance_share_links_resource
ON governance.share_links(workspace_id, resource_type, resource_id);

CREATE TABLE IF NOT EXISTS governance.audit_logs (
    id                    uuid            NOT NULL,
    workspace_id          uuid            NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    actor_user_id         uuid            REFERENCES identity.users(id),
    action                varchar(120)    NOT NULL,
    severity              varchar(40)     NOT NULL DEFAULT 'Info'
                          CHECK (severity IN ('Info', 'Warning', 'Critical')),
    resource_type         varchar(80),
    resource_id           uuid,
    before_json           jsonb,
    after_json            jsonb,
    metadata_json         jsonb           NOT NULL DEFAULT '{}'::jsonb,
    ip_address            inet,
    user_agent            text,
    correlation_id        varchar(120),
    occurred_at           timestamptz     NOT NULL DEFAULT now(),
    CONSTRAINT ck_governance_audit_logs_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

CREATE TABLE IF NOT EXISTS governance.audit_logs_y2025 PARTITION OF governance.audit_logs
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
CREATE TABLE IF NOT EXISTS governance.audit_logs_y2026 PARTITION OF governance.audit_logs
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
CREATE TABLE IF NOT EXISTS governance.audit_logs_default PARTITION OF governance.audit_logs DEFAULT;

CREATE INDEX IF NOT EXISTS ix_governance_audit_logs_workspace_time
ON governance.audit_logs(workspace_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS ix_governance_audit_logs_resource
ON governance.audit_logs(workspace_id, resource_type, resource_id);

CREATE TABLE IF NOT EXISTS governance.security_events (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    actor_user_id         uuid REFERENCES identity.users(id),
    event_type            varchar(120) NOT NULL,
    severity              varchar(40) NOT NULL DEFAULT 'Info'
                          CHECK (severity IN ('Info', 'Warning', 'Critical')),
    resource_type         varchar(80),
    resource_id           uuid,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    ip_address            inet,
    user_agent            text,
    occurred_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_governance_security_events_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE TABLE IF NOT EXISTS governance.workspace_policies (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL UNIQUE REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    allow_member_create_board boolean NOT NULL DEFAULT true,
    allow_member_invite  boolean NOT NULL DEFAULT false,
    allow_guest_comment  boolean NOT NULL DEFAULT false,
    allow_guest_export   boolean NOT NULL DEFAULT false,
    allow_public_links   boolean NOT NULL DEFAULT false,
    require_2fa_for_admin boolean NOT NULL DEFAULT false,
    policy_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at           timestamptz NOT NULL DEFAULT now(),
    updated_at           timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_governance_workspace_policies_updated_at
BEFORE UPDATE ON governance.workspace_policies
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS governance.custom_roles (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(160) NOT NULL,
    description           text,
    color                 varchar(20),
    is_system             boolean NOT NULL DEFAULT false,
    is_assignable         boolean NOT NULL DEFAULT true,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    CONSTRAINT ux_governance_custom_roles_name UNIQUE(workspace_id, name),
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE INDEX IF NOT EXISTS ix_governance_custom_roles_active ON governance.custom_roles(workspace_id) WHERE deleted_at IS NULL;

CREATE TRIGGER trg_governance_custom_roles_updated_at
BEFORE UPDATE ON governance.custom_roles
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS governance.custom_role_permissions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    custom_role_id        uuid NOT NULL REFERENCES governance.custom_roles(id) ON DELETE CASCADE,
    action                varchar(120) NOT NULL,
    is_allowed            boolean NOT NULL DEFAULT true,
    conditions_json       jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_governance_custom_role_permissions_action UNIQUE(custom_role_id, action)
);

CREATE TABLE IF NOT EXISTS governance.workspace_member_role_assignments (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    member_id             uuid NOT NULL REFERENCES workspace.workspace_members(id) ON DELETE CASCADE,
    custom_role_id        uuid NOT NULL REFERENCES governance.custom_roles(id) ON DELETE CASCADE,
    assigned_by_user_id   uuid REFERENCES identity.users(id),
    assigned_at           timestamptz NOT NULL DEFAULT now(),
    revoked_at            timestamptz,
    CONSTRAINT ux_governance_member_role_assignment UNIQUE(member_id, custom_role_id)
);

CREATE TABLE IF NOT EXISTS governance.permission_templates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(160) NOT NULL,
    description           text,
    resource_type         varchar(80),
    permissions_json      jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_system             boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_governance_permission_templates_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_governance_permission_templates_updated_at
BEFORE UPDATE ON governance.permission_templates
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS governance.resource_permission_inheritance_cache (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    parent_resource_type  varchar(80),
    parent_resource_id    uuid,
    subject_type          varchar(80) NOT NULL CHECK (subject_type IN ('User','WorkspaceRole','CustomRole','Team','Guest','PublicLink','ExternalEmail','ApiToken','System')),
    subject_id            uuid,
    subject_key           varchar(160),
    action                varchar(160) NOT NULL,
    effect                varchar(20) NOT NULL DEFAULT 'Allow' CHECK (effect IN ('Allow','Deny')),
    permission_level      varchar(40) CHECK (permission_level IS NULL OR permission_level IN ('None','View','Comment','Edit','Manage','Owner')),
    source_type           varchar(80) CHECK (source_type IS NULL OR source_type IN ('Direct','Role','Team','Workspace','ParentResource','Policy','Template','System')),
    source_id             uuid,
    inherited_from_resource_type varchar(80),
    inherited_from_resource_id uuid,
    cache_version         bigint NOT NULL DEFAULT 1,
    computed_permissions_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    computed_at           timestamptz NOT NULL DEFAULT now(),
    expires_at            timestamptz,
    CONSTRAINT ck_governance_inheritance_cache_subject_identity CHECK (subject_id IS NOT NULL OR subject_key IS NOT NULL OR subject_type = 'System'),
    CONSTRAINT ck_governance_inheritance_cache_resource_type CHECK (resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT ck_governance_inheritance_cache_parent_type CHECK (parent_resource_type IS NULL OR parent_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT ck_governance_inheritance_cache_inherited_from_type CHECK (inherited_from_resource_type IS NULL OR inherited_from_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_governance_permission_inheritance_cache
ON governance.resource_permission_inheritance_cache(
    workspace_id,
    resource_type,
    resource_id,
    subject_type,
    COALESCE(subject_id, '00000000-0000-0000-0000-000000000000'::uuid),
    COALESCE(subject_key, ''),
    action
);

CREATE INDEX IF NOT EXISTS ix_governance_permission_inheritance_cache_lookup
ON governance.resource_permission_inheritance_cache(workspace_id, subject_type, subject_id, resource_type, resource_id, action);

CREATE INDEX IF NOT EXISTS ix_governance_permission_inheritance_cache_subject_key
ON governance.resource_permission_inheritance_cache(workspace_id, subject_type, subject_key, resource_type, resource_id, action)
WHERE subject_key IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_governance_permission_inheritance_cache_resource
ON governance.resource_permission_inheritance_cache(workspace_id, resource_type, resource_id, cache_version);

CREATE INDEX IF NOT EXISTS ix_governance_permission_inheritance_cache_expiry
ON governance.resource_permission_inheritance_cache(expires_at)
WHERE expires_at IS NOT NULL;


CREATE TABLE IF NOT EXISTS governance.audit_retention_policies (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL UNIQUE REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    retention_days        integer NOT NULL DEFAULT 365,
    export_before_delete  boolean NOT NULL DEFAULT false,
    policy_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_governance_audit_retention_policies_updated_at
BEFORE UPDATE ON governance.audit_retention_policies
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 04. work schema
-- Service candidate: WorkManagement Service
-- Owns: boards, fields, items, values, views, labels, checklists
-- =============================================================================

CREATE TABLE IF NOT EXISTS work.boards (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    space_id              uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    name                  varchar(180) NOT NULL,
    description           text,
    icon                  varchar(80),
    color                 varchar(20),
    board_type            varchar(60) NOT NULL DEFAULT 'WorkManagement'
                          CHECK (board_type IN ('WorkManagement', 'Roadmap', 'DashboardSource')),
    visibility            varchar(40) NOT NULL DEFAULT 'Workspace'
                          CHECK (visibility IN ('Private', 'Workspace', 'PublicLink')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    item_key_prefix varchar(32),
    item_sequence bigint NOT NULL DEFAULT 0,
    default_item_group_id uuid,
    board_family varchar(40) NOT NULL DEFAULT 'Core',
    CONSTRAINT ux_work_boards_workspace_id_id UNIQUE(workspace_id, id)
);
CREATE TRIGGER trg_work_boards_updated_at
BEFORE UPDATE ON work.boards
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_work_boards_workspace ON work.boards(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_boards_space ON work.boards(space_id);
CREATE INDEX IF NOT EXISTS ix_work_boards_name_trgm ON work.boards USING gin(name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_work_boards_active ON work.boards(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS work.board_groups (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    name                  varchar(160) NOT NULL,
    color                 varchar(20),
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    is_collapsed          boolean NOT NULL DEFAULT false,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ux_work_board_groups_workspace_board_id_id UNIQUE(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_groups_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id));
CREATE TRIGGER trg_work_board_groups_updated_at
BEFORE UPDATE ON work.board_groups
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_work_board_groups_board_position ON work.board_groups(board_id, position);
CREATE INDEX IF NOT EXISTS ix_work_board_groups_active ON work.board_groups(board_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS work.board_fields (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    key                   varchar(120) NOT NULL,
    name                  varchar(160) NOT NULL,
    field_type            varchar(60) NOT NULL,
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    default_value_json    jsonb,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    is_required           boolean NOT NULL DEFAULT false,
    is_system             boolean NOT NULL DEFAULT false,
    is_hidden             boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    CONSTRAINT ux_work_board_fields_board_key UNIQUE(board_id, key),
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    data_classification varchar(40) NOT NULL DEFAULT 'Internal',
    is_sensitive boolean NOT NULL DEFAULT false,
    is_formula boolean NOT NULL DEFAULT false,
    formula_expression text,
    mirror_source_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT ux_work_board_fields_workspace_board_id_id UNIQUE(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_fields_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id)
);
CREATE TRIGGER trg_work_board_fields_updated_at
BEFORE UPDATE ON work.board_fields
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_work_board_fields_board_position ON work.board_fields(board_id, position) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_fields_active ON work.board_fields(board_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS work.field_options (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    field_id              uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    value                 varchar(160) NOT NULL,
    label                 varchar(160) NOT NULL,
    color                 varchar(20),
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    is_archived           boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    CONSTRAINT ux_work_field_options_value UNIQUE(field_id, value),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_field_options_workspace_board_field_id UNIQUE(workspace_id, board_id, field_id, id),
    CONSTRAINT fk_work_field_options_workspace_field FOREIGN KEY (workspace_id, board_id, field_id) REFERENCES work.board_fields(workspace_id, board_id, id));
CREATE TRIGGER trg_work_field_options_updated_at
BEFORE UPDATE ON work.field_options
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.board_views (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    owner_user_id         uuid REFERENCES identity.users(id),
    name                  varchar(160) NOT NULL,
    view_type             varchar(60) NOT NULL CHECK (view_type IN ('Table', 'Kanban', 'Calendar', 'Timeline', 'Gantt', 'Form', 'Dashboard')),
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_default            boolean NOT NULL DEFAULT false,
    is_private            boolean NOT NULL DEFAULT false,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT fk_work_board_views_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id)
);
CREATE TRIGGER trg_work_board_views_updated_at
BEFORE UPDATE ON work.board_views
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE UNIQUE INDEX IF NOT EXISTS ux_work_board_views_default ON work.board_views(board_id)
WHERE is_default = true AND deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS work.board_items (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    group_id              uuid REFERENCES work.board_groups(id) ON DELETE SET NULL,
    name                  varchar(300) NOT NULL,
    description_markdown  text,
    values_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    is_archived           boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    parent_item_id uuid,
    item_key varchar(80),
    item_sequence bigint,
    item_level smallint NOT NULL DEFAULT 0,
    started_at timestamptz,
    due_at timestamptz,
    completed_at timestamptz,
    CONSTRAINT ux_work_board_items_workspace_board_id_id UNIQUE(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_items_workspace_board_group FOREIGN KEY (workspace_id, board_id, group_id) REFERENCES work.board_groups(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_items_parent_scope FOREIGN KEY (workspace_id, board_id, parent_item_id) REFERENCES work.board_items(workspace_id, board_id, id));
CREATE TRIGGER trg_work_board_items_updated_at
BEFORE UPDATE ON work.board_items
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_work_board_items_board_group_position ON work.board_items(board_id, group_id, position) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_items_values_json ON work.board_items USING gin(values_json);
CREATE INDEX IF NOT EXISTS ix_work_board_items_name_trgm ON work.board_items USING gin(name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_work_board_items_active ON work.board_items(board_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS work.board_item_values (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    field_id              uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    value_json            jsonb,
    value_text            text,
    value_number          numeric,
    value_bool            boolean,
    value_date            date,
    value_datetime        timestamptz,
    value_user_ids        uuid[],
    updated_at            timestamptz NOT NULL DEFAULT now(),
    updated_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_work_board_item_values_item_field UNIQUE(item_id, field_id),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT fk_work_board_item_values_workspace_field FOREIGN KEY (workspace_id, board_id, field_id) REFERENCES work.board_fields(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_item_values_workspace_item FOREIGN KEY (workspace_id, board_id, item_id) REFERENCES work.board_items(workspace_id, board_id, id));
CREATE TRIGGER trg_work_board_item_values_updated_at
BEFORE UPDATE ON work.board_item_values
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_work_board_item_values_field_text ON work.board_item_values(field_id, value_text);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_field_number ON work.board_item_values(field_id, value_number);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_field_date ON work.board_item_values(field_id, value_date);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_field_datetime ON work.board_item_values(field_id, value_datetime);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_json ON work.board_item_values USING gin(value_json);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_workspace_item ON work.board_item_values(workspace_id, board_id, item_id);

CREATE TABLE IF NOT EXISTS work.board_item_members (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    assigned_by_user_id   uuid REFERENCES identity.users(id),
    assigned_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_work_board_item_members_item_user UNIQUE(item_id, user_id),
    CONSTRAINT fk_work_board_item_members_workspace_item FOREIGN KEY (workspace_id, board_id, item_id) REFERENCES work.board_items(workspace_id, board_id, id));

CREATE TABLE IF NOT EXISTS work.labels (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    name                  varchar(120) NOT NULL,
    color                 varchar(20),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    CONSTRAINT ux_work_labels_board_name UNIQUE(board_id, name),
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_labels_workspace_board_id_id UNIQUE(workspace_id, board_id, id),
    CONSTRAINT fk_work_labels_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id));
CREATE TRIGGER trg_work_labels_updated_at
BEFORE UPDATE ON work.labels
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.board_item_labels (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    label_id              uuid NOT NULL REFERENCES work.labels(id) ON DELETE CASCADE,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_work_board_item_labels_item_label UNIQUE(item_id, label_id),
    CONSTRAINT fk_work_board_item_labels_workspace_item FOREIGN KEY (workspace_id, board_id, item_id) REFERENCES work.board_items(workspace_id, board_id, id),
    CONSTRAINT fk_work_board_item_labels_workspace_label FOREIGN KEY (workspace_id, board_id, label_id) REFERENCES work.labels(workspace_id, board_id, id));

CREATE TABLE IF NOT EXISTS work.board_item_links (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    source_item_id        uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    target_resource_type  varchar(80) NOT NULL,
    target_resource_id    uuid NOT NULL,
    link_type             varchar(60) NOT NULL DEFAULT 'Related'
                          CHECK (link_type IN ('Related', 'Blocks', 'BlockedBy', 'Parent', 'Child', 'Duplicate', 'Mention')),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_work_board_item_links_unique UNIQUE(source_item_id, target_resource_type, target_resource_id, link_type),
    CONSTRAINT ck_work_board_item_links_target_resource_type CHECK (target_resource_type IS NULL OR target_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT fk_work_board_item_links_workspace_source FOREIGN KEY (workspace_id, board_id, source_item_id) REFERENCES work.board_items(workspace_id, board_id, id));

CREATE TABLE IF NOT EXISTS work.checklists (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    title                 varchar(200) NOT NULL DEFAULT 'Checklist',
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_checklists_workspace_board_item_id UNIQUE(workspace_id, board_id, item_id, id),
    CONSTRAINT fk_work_checklists_workspace_item FOREIGN KEY (workspace_id, board_id, item_id) REFERENCES work.board_items(workspace_id, board_id, id),
    CONSTRAINT ux_work_checklists_workspace_id UNIQUE(workspace_id, id));
CREATE TRIGGER trg_work_checklists_updated_at
BEFORE UPDATE ON work.checklists
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.checklist_items (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    checklist_id          uuid NOT NULL REFERENCES work.checklists(id) ON DELETE CASCADE,
    title                 varchar(300) NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Done')),
    assignee_user_id      uuid REFERENCES identity.users(id),
    due_at                timestamptz,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    completed_at          timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT fk_work_checklist_items_workspace_checklist FOREIGN KEY (workspace_id, checklist_id) REFERENCES work.checklists(workspace_id, id));
CREATE TRIGGER trg_work_checklist_items_updated_at
BEFORE UPDATE ON work.checklist_items
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.board_view_user_preferences (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    view_id               uuid NOT NULL REFERENCES work.board_views(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    preferences_json      jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_work_board_view_user_preferences UNIQUE(view_id, user_id),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT fk_work_board_view_user_preferences_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id));
CREATE TRIGGER trg_work_board_view_user_preferences_updated_at
BEFORE UPDATE ON work.board_view_user_preferences
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.saved_filters (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid REFERENCES work.boards(id) ON DELETE CASCADE,
    user_id               uuid REFERENCES identity.users(id) ON DELETE CASCADE,
    name                  varchar(160) NOT NULL,
    filter_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_shared             boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT fk_work_saved_filters_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id));
CREATE TRIGGER trg_work_saved_filters_updated_at
BEFORE UPDATE ON work.saved_filters
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.relation_field_configs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    field_id              uuid NOT NULL UNIQUE REFERENCES work.board_fields(id) ON DELETE CASCADE,
    source_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    target_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    allow_multiple        boolean NOT NULL DEFAULT true,
    create_backlink       boolean NOT NULL DEFAULT true,
    backlink_field_id     uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_work_relation_field_configs_updated_at
BEFORE UPDATE ON work.relation_field_configs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.formula_dependencies (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    formula_field_id      uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    depends_on_field_id   uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_work_formula_dependencies UNIQUE(formula_field_id, depends_on_field_id)
);

CREATE TABLE IF NOT EXISTS work.rollup_snapshots (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    field_id              uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    value_json            jsonb,
    calculated_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_work_rollup_snapshots_item_field UNIQUE(item_id, field_id)
);

CREATE TABLE IF NOT EXISTS work.approval_requests (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    title                 varchar(240) NOT NULL,
    description           text,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
    requested_by_user_id  uuid NOT NULL REFERENCES identity.users(id),
    decided_by_user_id    uuid REFERENCES identity.users(id),
    decided_at            timestamptz,
    decision_note         text,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_work_approval_requests_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_work_approval_requests_updated_at
BEFORE UPDATE ON work.approval_requests
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.approval_steps (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    approval_request_id   uuid NOT NULL REFERENCES work.approval_requests(id) ON DELETE CASCADE,
    approver_user_id      uuid REFERENCES identity.users(id),
    approver_team_id      uuid REFERENCES workspace.teams(id),
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Approved', 'Rejected', 'Skipped')),
    position              integer NOT NULL DEFAULT 0,
    decided_at            timestamptz,
    note                  text,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS work.workload_allocations (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid REFERENCES work.board_items(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    allocation_date       date NOT NULL,
    capacity_minutes      integer NOT NULL DEFAULT 0,
    allocated_minutes     integer NOT NULL DEFAULT 0,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_work_workload_allocations_user_date_item UNIQUE(workspace_id, user_id, allocation_date, item_id),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_work_workload_allocations_updated_at
BEFORE UPDATE ON work.workload_allocations
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 05. docs schema
-- Service candidate: Document Service
-- Owns: pages, blocks, document versions, resource links, page templates
-- =============================================================================

CREATE TABLE IF NOT EXISTS docs.pages (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    space_id              uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    parent_page_id        uuid REFERENCES docs.pages(id) ON DELETE SET NULL,
    title                 varchar(240) NOT NULL,
    slug                  varchar(260),
    icon                  varchar(80),
    cover_url             text,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    visibility            varchar(40) NOT NULL DEFAULT 'Workspace'
                          CHECK (visibility IN ('Private', 'Workspace', 'PublicLink')),
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    properties_json       jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    CONSTRAINT ux_docs_pages_workspace_slug UNIQUE(workspace_id, slug),
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE TRIGGER trg_docs_pages_updated_at
BEFORE UPDATE ON docs.pages
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_docs_pages_workspace_parent_position ON docs.pages(workspace_id, parent_page_id, position);
CREATE INDEX IF NOT EXISTS ix_docs_pages_title_trgm ON docs.pages USING gin(title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_docs_pages_active ON docs.pages(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS docs.blocks (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    page_id               uuid NOT NULL REFERENCES docs.pages(id) ON DELETE CASCADE,
    parent_block_id       uuid REFERENCES docs.blocks(id) ON DELETE CASCADE,
    block_type            varchar(60) NOT NULL,
    content_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    properties_json       jsonb NOT NULL DEFAULT '{}'::jsonb,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    version               bigint NOT NULL DEFAULT 1,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ux_docs_blocks_workspace_page_id_id UNIQUE(workspace_id, page_id, id)
);
CREATE TRIGGER trg_docs_blocks_updated_at
BEFORE UPDATE ON docs.blocks
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_docs_blocks_page_parent_position ON docs.blocks(page_id, parent_block_id, position) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_docs_blocks_content_json ON docs.blocks USING gin(content_json);
CREATE INDEX IF NOT EXISTS ix_docs_blocks_active ON docs.blocks(page_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS docs.document_versions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    page_id               uuid NOT NULL REFERENCES docs.pages(id) ON DELETE CASCADE,
    version_number        bigint NOT NULL,
    snapshot_json         jsonb NOT NULL,
    created_by            uuid REFERENCES identity.users(id),
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_docs_document_versions_page_version UNIQUE(page_id, version_number)
);

CREATE TABLE IF NOT EXISTS docs.resource_links (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    source_resource_type  varchar(80) NOT NULL,
    source_resource_id    uuid NOT NULL,
    target_resource_type  varchar(80) NOT NULL,
    target_resource_id    uuid NOT NULL,
    link_type             varchar(60) NOT NULL DEFAULT 'Related'
                          CHECK (link_type IN ('Related', 'Mention', 'Embed', 'Dependency', 'Backlink')),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_docs_resource_links_unique
        UNIQUE(source_resource_type, source_resource_id, target_resource_type, target_resource_id, link_type),
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    CONSTRAINT ck_docs_resource_links_source_resource_type CHECK (source_resource_type IS NULL OR source_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT ck_docs_resource_links_target_resource_type CHECK (target_resource_type IS NULL OR target_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE TABLE IF NOT EXISTS docs.page_templates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    description           text,
    category              varchar(120),
    is_system             boolean NOT NULL DEFAULT false,
    is_public             boolean NOT NULL DEFAULT false,
    page_snapshot_json    jsonb NOT NULL,
    blocks_snapshot_json  jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_docs_page_templates_updated_at
BEFORE UPDATE ON docs.page_templates
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 06. collab schema
-- Service candidates: Collaboration Service + Notification Service
-- Owns: comments, reactions, mentions, notifications, activity, attachments, presence
-- =============================================================================

CREATE TABLE IF NOT EXISTS collab.comments (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    parent_comment_id     uuid REFERENCES collab.comments(id) ON DELETE CASCADE,
    author_user_id        uuid NOT NULL REFERENCES identity.users(id),
    body_markdown         text NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Deleted', 'Resolved')),
    anchor_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ck_collab_comments_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_collab_comments_updated_at
BEFORE UPDATE ON collab.comments
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_collab_comments_resource ON collab.comments(workspace_id, resource_type, resource_id, created_at);
CREATE INDEX IF NOT EXISTS ix_collab_comments_active ON collab.comments(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS collab.reactions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    user_id               uuid NOT NULL REFERENCES identity.users(id),
    emoji                 varchar(40) NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_collab_reactions_resource_user_emoji UNIQUE(resource_type, resource_id, user_id, emoji),
    CONSTRAINT ck_collab_reactions_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE TABLE IF NOT EXISTS collab.mentions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    mentioned_user_id     uuid NOT NULL REFERENCES identity.users(id),
    mentioned_by_user_id  uuid REFERENCES identity.users(id),
    mention_type          varchar(40) NOT NULL DEFAULT 'User' CHECK (mention_type IN ('User', 'Team', 'Role')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_collab_mentions_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE TABLE IF NOT EXISTS collab.notifications (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    recipient_user_id     uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    notification_type     varchar(80) NOT NULL,
    title                 varchar(240) NOT NULL,
    body                  text,
    resource_type         varchar(80),
    resource_id           uuid,
    payload_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    status                varchar(40) NOT NULL DEFAULT 'Unread'
                          CHECK (status IN ('Unread', 'Read', 'Archived')),
    read_at               timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_collab_notifications_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    updated_at timestamptz,
    updated_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1);
CREATE TRIGGER trg_collab_notifications_updated_at
BEFORE UPDATE ON collab.notifications
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_collab_notifications_recipient_status_created ON collab.notifications(recipient_user_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS collab.notification_preferences (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    notification_type     varchar(120) NOT NULL,
    channel               varchar(40) NOT NULL CHECK (channel IN ('InApp', 'Email', 'Push', 'Slack')),
    is_enabled            boolean NOT NULL DEFAULT true,
    quiet_hours_json      jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_collab_notification_preferences UNIQUE(workspace_id, user_id, notification_type, channel),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_collab_notification_preferences_updated_at
BEFORE UPDATE ON collab.notification_preferences
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS collab.notification_deliveries (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    notification_id       uuid NOT NULL REFERENCES collab.notifications(id) ON DELETE CASCADE,
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    recipient_user_id     uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    channel               varchar(40) NOT NULL CHECK (channel IN ('InApp', 'Email', 'Push', 'Slack')),
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Sent', 'Failed', 'Skipped', 'Cancelled')),
    provider_message_id   varchar(255),
    error_message         text,
    sent_at               timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS collab.unread_counters (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    counter_type          varchar(80) NOT NULL DEFAULT 'Notification'
                          CHECK (counter_type IN ('Notification', 'Mention', 'AssignedItem')),
    counter_value         integer NOT NULL DEFAULT 0,
    updated_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_collab_unread_counters_user_type UNIQUE(workspace_id, user_id, counter_type),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_collab_unread_counters_updated_at
BEFORE UPDATE ON collab.unread_counters
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS collab.activity_logs (
    id                    uuid            NOT NULL,
    workspace_id          uuid            NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    actor_user_id         uuid            REFERENCES identity.users(id),
    activity_type         varchar(120)    NOT NULL,
    resource_type         varchar(80),
    resource_id           uuid,
    target_resource_type  varchar(80),
    target_resource_id    uuid,
    summary               text,
    metadata_json         jsonb           NOT NULL DEFAULT '{}'::jsonb,
    occurred_at           timestamptz     NOT NULL DEFAULT now(),
    is_visible            boolean         NOT NULL DEFAULT true,
    hidden_at             timestamptz,
    hidden_by             uuid            REFERENCES identity.users(id),
    hidden_reason         text,
    CONSTRAINT ck_collab_activity_logs_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT ck_collab_activity_logs_target_resource_type CHECK (target_resource_type IS NULL OR target_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

-- Monthly partitions (auto-managed; extend via migration or pg_partman)
CREATE TABLE IF NOT EXISTS collab.activity_logs_y2025m01 PARTITION OF collab.activity_logs
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');
CREATE TABLE IF NOT EXISTS collab.activity_logs_y2025m06 PARTITION OF collab.activity_logs
    FOR VALUES FROM ('2025-06-01') TO ('2025-07-01');
CREATE TABLE IF NOT EXISTS collab.activity_logs_y2025m07 PARTITION OF collab.activity_logs
    FOR VALUES FROM ('2025-07-01') TO ('2026-01-01');
CREATE TABLE IF NOT EXISTS collab.activity_logs_y2026 PARTITION OF collab.activity_logs
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
CREATE TABLE IF NOT EXISTS collab.activity_logs_default PARTITION OF collab.activity_logs DEFAULT;

CREATE INDEX IF NOT EXISTS ix_collab_activity_logs_workspace_time ON collab.activity_logs(workspace_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_activity_logs_resource ON collab.activity_logs(resource_type, resource_id, occurred_at DESC);

CREATE TABLE IF NOT EXISTS collab.attachments (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    file_name             varchar(260) NOT NULL,
    content_type          varchar(160),
    size_bytes            bigint NOT NULL DEFAULT 0,
    storage_key           text NOT NULL,
    public_url            text,
    uploaded_by_user_id   uuid REFERENCES identity.users(id),
    uploaded_at           timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz,
    CONSTRAINT ck_collab_attachments_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    updated_at timestamptz,
    updated_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL);
CREATE TRIGGER trg_collab_attachments_updated_at
BEFORE UPDATE ON collab.attachments
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_collab_attachments_resource ON collab.attachments(workspace_id, resource_type, resource_id);
CREATE INDEX IF NOT EXISTS ix_collab_attachments_active ON collab.attachments(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS collab.resource_watchers (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    watch_level           varchar(40) NOT NULL DEFAULT 'All'
                          CHECK (watch_level IN ('All', 'MentionsOnly', 'None')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_collab_resource_watchers_resource_user UNIQUE(resource_type, resource_id, user_id),
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    CONSTRAINT ck_collab_resource_watchers_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    updated_at timestamptz,
    updated_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1);
CREATE TRIGGER trg_collab_resource_watchers_updated_at
BEFORE UPDATE ON collab.resource_watchers
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS collab.presence_sessions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    resource_type         varchar(80),
    resource_id           uuid,
    connection_id         varchar(160),
    status                varchar(40) NOT NULL DEFAULT 'Online'
                          CHECK (status IN ('Online', 'Idle', 'Offline')),
    last_seen_at          timestamptz NOT NULL DEFAULT now(),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT ck_collab_presence_sessions_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);


-- =============================================================================
-- 07. automation schema
-- Service candidate: Automation Service / Worker
-- Owns: automation rules, executions, scheduled jobs, outbox
-- =============================================================================

CREATE TABLE IF NOT EXISTS automation.automation_rules (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid REFERENCES work.boards(id) ON DELETE CASCADE,
    name                  varchar(200) NOT NULL,
    description           text,
    status                varchar(40) NOT NULL DEFAULT 'Enabled'
                          CHECK (status IN ('Enabled', 'Disabled', 'Deleted')),
    trigger_json          jsonb NOT NULL,
    conditions_json       jsonb NOT NULL DEFAULT '[]'::jsonb,
    actions_json          jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ux_automation_rules_workspace_id_id UNIQUE(workspace_id, id)
);
CREATE TRIGGER trg_automation_automation_rules_updated_at
BEFORE UPDATE ON automation.automation_rules
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_automation_rules_workspace ON automation.automation_rules(workspace_id, status);
CREATE INDEX IF NOT EXISTS ix_automation_rules_board ON automation.automation_rules(board_id, status);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_active ON automation.automation_rules(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS automation.automation_executions (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    automation_rule_id    uuid REFERENCES automation.automation_rules(id) ON DELETE SET NULL,
    trigger_event_id      uuid,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Running', 'Succeeded', 'Failed', 'Skipped')),
    input_json            jsonb NOT NULL DEFAULT '{}'::jsonb,
    result_json           jsonb,
    error_message         text,
    started_at            timestamptz,
    completed_at          timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS automation.scheduled_jobs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type              varchar(120) NOT NULL,
    resource_type         varchar(80),
    resource_id           uuid,
    schedule_kind         varchar(40) NOT NULL DEFAULT 'Cron'
                          CHECK (schedule_kind IN ('Cron', 'Once', 'Interval')),
    cron_expression       varchar(160),
    interval_seconds      integer,
    run_at                timestamptz,
    timezone              varchar(80) DEFAULT 'UTC',
    payload_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Paused', 'Completed', 'Cancelled', 'Deleted')),
    last_run_at           timestamptz,
    next_run_at           timestamptz,
    locked_by             varchar(120),
    locked_until          timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_automation_scheduled_jobs_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_automation_scheduled_jobs_updated_at
BEFORE UPDATE ON automation.scheduled_jobs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_automation_scheduled_jobs_due
ON automation.scheduled_jobs(status, next_run_at)
WHERE status = 'Active';

CREATE TABLE IF NOT EXISTS automation.automation_templates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    description           text,
    category              varchar(120),
    is_system             boolean NOT NULL DEFAULT false,
    is_public             boolean NOT NULL DEFAULT false,
    trigger_json          jsonb NOT NULL,
    conditions_json       jsonb NOT NULL DEFAULT '[]'::jsonb,
    actions_json          jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_automation_automation_templates_updated_at
BEFORE UPDATE ON automation.automation_templates
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS automation.outbox_messages (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid,
    aggregate_type        varchar(160) NOT NULL,
    aggregate_id          uuid,
    event_type            varchar(240) NOT NULL,
    event_payload_json    jsonb NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Processing', 'Processed', 'Failed', 'DeadLetter')),
    retry_count           integer NOT NULL DEFAULT 0,
    next_attempt_at       timestamptz,
    last_attempt_at       timestamptz,
    processed_at          timestamptz,
    error_message         text,
    locked_by             varchar(120),
    locked_until          timestamptz,
    occurred_at           timestamptz NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    event_version integer NOT NULL DEFAULT 1,
    actor_user_id uuid REFERENCES identity.users(id),
    correlation_id uuid,
    causation_id uuid,
    idempotency_key varchar(200),
    partition_key varchar(160)
);

CREATE INDEX IF NOT EXISTS ix_automation_outbox_pending
ON automation.outbox_messages(status, locked_until, next_attempt_at, created_at)
WHERE status IN ('Pending', 'Failed', 'Processing');


-- =============================================================================
-- 08. integration schema
-- Service candidate: Integration Service
-- =============================================================================

CREATE TABLE IF NOT EXISTS integration.integration_connections (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider              varchar(80) NOT NULL CHECK (provider IN ('Google', 'Microsoft', 'Slack', 'GitHub', 'Linear', 'Custom')),
    name                  varchar(160) NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Disabled', 'Error', 'Deleted')),
    credentials_ref       text,
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    last_synced_at        timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE TRIGGER trg_integration_integration_connections_updated_at
BEFORE UPDATE ON integration.integration_connections
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_integration_connections_workspace_provider ON integration.integration_connections(workspace_id, provider, status);
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_active ON integration.integration_connections(workspace_id) WHERE deleted_at IS NULL;


CREATE TABLE IF NOT EXISTS integration.integration_scopes (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id         uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    scope                 varchar(160) NOT NULL,
    granted_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_integration_scopes_connection_scope UNIQUE(connection_id, scope),
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS integration.integration_secret_versions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id         uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    secret_ref            text NOT NULL,
    version_number        integer NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Rotated', 'Revoked')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    revoked_at            timestamptz,
    CONSTRAINT ux_integration_secret_versions UNIQUE(connection_id, version_number),
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS integration.webhook_subscriptions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id         uuid REFERENCES integration.integration_connections(id) ON DELETE SET NULL,
    name                  varchar(160) NOT NULL,
    target_url            text NOT NULL,
    secret_hash           text,
    event_types           text[] NOT NULL DEFAULT ARRAY[]::text[],
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Disabled', 'Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id)
);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_subscriptions_active ON integration.webhook_subscriptions(workspace_id) WHERE deleted_at IS NULL;

CREATE TRIGGER trg_integration_webhook_subscriptions_updated_at
BEFORE UPDATE ON integration.webhook_subscriptions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS integration.webhook_deliveries (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    webhook_subscription_id uuid NOT NULL REFERENCES integration.webhook_subscriptions(id) ON DELETE CASCADE,
    event_type            varchar(160) NOT NULL,
    payload_json          jsonb NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Succeeded', 'Failed', 'Retrying', 'DeadLetter')),
    response_status_code  integer,
    response_body         text,
    retry_count           integer NOT NULL DEFAULT 0,
    next_retry_at         timestamptz,
    delivered_at          timestamptz,
    error_message         text,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS integration.inbound_webhook_events (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id         uuid REFERENCES integration.integration_connections(id) ON DELETE SET NULL,
    provider              varchar(80) NOT NULL,
    external_event_id     varchar(255),
    event_type            varchar(160) NOT NULL,
    payload_json          jsonb NOT NULL,
    headers_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Processed', 'Failed', 'Ignored')),
    processed_at          timestamptz,
    error_message         text,
    received_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_integration_inbound_webhook_events_external UNIQUE(provider, external_event_id)
);

CREATE TABLE IF NOT EXISTS integration.calendar_integrations (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id         uuid REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    provider              varchar(40) NOT NULL CHECK (provider IN ('Google', 'Microsoft')),
    calendar_external_id  varchar(255) NOT NULL,
    sync_direction        varchar(40) NOT NULL DEFAULT 'OneWayToExternal'
                          CHECK (sync_direction IN ('OneWayToExternal', 'OneWayToNotrelix', 'TwoWay')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Disabled', 'Error')),
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    last_synced_at        timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_integration_calendar_integrations_updated_at
BEFORE UPDATE ON integration.calendar_integrations
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS integration.calendar_event_links (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    calendar_integration_id uuid NOT NULL REFERENCES integration.calendar_integrations(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    external_event_id     varchar(255) NOT NULL,
    last_synced_at        timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_integration_calendar_event_links_external UNIQUE(calendar_integration_id, external_event_id),
    CONSTRAINT ux_integration_calendar_event_links_resource UNIQUE(calendar_integration_id, resource_type, resource_id),
    CONSTRAINT ck_integration_calendar_event_links_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE TABLE IF NOT EXISTS integration.integration_sync_cursors (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id         uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    cursor_value          text,
    last_synced_at        timestamptz,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_integration_sync_cursors UNIQUE(connection_id, resource_type),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_integration_sync_cursors_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_integration_integration_sync_cursors_updated_at
BEFORE UPDATE ON integration.integration_sync_cursors
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 09. billing schema
-- Service candidate: Billing Service
-- =============================================================================

CREATE TABLE IF NOT EXISTS billing.plans (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code                  varchar(80) NOT NULL UNIQUE,
    name                  varchar(160) NOT NULL,
    description           text,
    price_cents           integer NOT NULL DEFAULT 0,
    currency              varchar(10) NOT NULL DEFAULT 'USD',
    billing_period        varchar(40) NOT NULL DEFAULT 'Monthly'
                          CHECK (billing_period IN ('Free', 'Monthly', 'Yearly')),
    is_active             boolean NOT NULL DEFAULT true,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_billing_plans_updated_at
BEFORE UPDATE ON billing.plans
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS billing.plan_limits (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id               uuid NOT NULL REFERENCES billing.plans(id) ON DELETE CASCADE,
    feature_code          varchar(120) NOT NULL,
    limit_value           integer,
    is_enabled            boolean NOT NULL DEFAULT true,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT ux_billing_plan_limits_feature UNIQUE(plan_id, feature_code)
);

CREATE TABLE IF NOT EXISTS billing.subscriptions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL UNIQUE REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    plan_id               uuid NOT NULL REFERENCES billing.plans(id),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Trialing', 'Active', 'PastDue', 'Cancelled', 'Expired')),
    started_at            timestamptz NOT NULL DEFAULT now(),
    current_period_start  timestamptz,
    current_period_end    timestamptz,
    cancelled_at          timestamptz,
    external_customer_id  varchar(255),
    external_subscription_id varchar(255),
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_billing_subscriptions_workspace_id_id UNIQUE(workspace_id, id)
);
CREATE TRIGGER trg_billing_subscriptions_updated_at
BEFORE UPDATE ON billing.subscriptions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS billing.payment_methods (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider              varchar(80) NOT NULL DEFAULT 'Stripe',
    external_payment_method_id varchar(255) NOT NULL,
    brand                 varchar(80),
    last4                 varchar(10),
    exp_month             integer,
    exp_year              integer,
    is_default            boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_billing_payment_methods_external UNIQUE(provider, external_payment_method_id),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_billing_payment_methods_updated_at
BEFORE UPDATE ON billing.payment_methods
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE UNIQUE INDEX IF NOT EXISTS ux_billing_payment_methods_default ON billing.payment_methods(workspace_id) WHERE is_default = true;

CREATE TABLE IF NOT EXISTS billing.invoices (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider              varchar(80) NOT NULL DEFAULT 'Stripe',
    external_invoice_id   varchar(255),
    status                varchar(40) NOT NULL DEFAULT 'Draft'
                          CHECK (status IN ('Draft', 'Open', 'Paid', 'Void', 'Uncollectible')),
    currency              varchar(10) NOT NULL DEFAULT 'USD',
    amount_due_cents      integer NOT NULL DEFAULT 0,
    amount_paid_cents     integer NOT NULL DEFAULT 0,
    invoice_url           text,
    period_start          timestamptz,
    period_end            timestamptz,
    due_at                timestamptz,
    paid_at               timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_billing_invoices_external UNIQUE(provider, external_invoice_id)
);

CREATE TABLE IF NOT EXISTS billing.billing_events (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider              varchar(80) NOT NULL DEFAULT 'Stripe',
    external_event_id     varchar(255),
    event_type            varchar(160) NOT NULL,
    payload_json          jsonb NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Processed', 'Failed', 'Ignored')),
    processed_at          timestamptz,
    error_message         text,
    received_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_billing_events_external UNIQUE(provider, external_event_id)
);

CREATE TABLE IF NOT EXISTS billing.usage_metrics (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    metric_key            varchar(120) NOT NULL,
    metric_value          bigint NOT NULL DEFAULT 0,
    measured_at           timestamptz NOT NULL DEFAULT now(),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT ux_billing_usage_metrics_workspace_key UNIQUE(workspace_id, metric_key),
    version bigint NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS billing.usage_metric_history (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    metric_key            varchar(120) NOT NULL,
    metric_value          bigint NOT NULL DEFAULT 0,
    period_start          timestamptz NOT NULL,
    period_end            timestamptz NOT NULL,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS billing.entitlements (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code          varchar(120) NOT NULL,
    is_enabled            boolean NOT NULL DEFAULT true,
    limit_value           integer,
    source                varchar(80) NOT NULL DEFAULT 'Plan'
                          CHECK (source IN ('Plan', 'Override', 'Trial', 'Promotion')),
    expires_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_billing_entitlements_workspace_feature UNIQUE(workspace_id, feature_code),
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_billing_entitlements_updated_at
BEFORE UPDATE ON billing.entitlements
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 10. reporting schema
-- Service candidate: Reporting Service
-- =============================================================================

CREATE TABLE IF NOT EXISTS reporting.dashboards (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    space_id              uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    name                  varchar(180) NOT NULL,
    description           text,
    icon                  varchar(80),
    visibility            varchar(40) NOT NULL DEFAULT 'Workspace'
                          CHECK (visibility IN ('Private', 'Workspace', 'PublicLink')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active', 'Archived', 'Deleted')),
    layout_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ux_reporting_dashboards_workspace_id UNIQUE(workspace_id, id));
CREATE INDEX IF NOT EXISTS ix_reporting_dashboards_active ON reporting.dashboards(workspace_id) WHERE deleted_at IS NULL;

CREATE TRIGGER trg_reporting_dashboards_updated_at
BEFORE UPDATE ON reporting.dashboards
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS reporting.dashboard_widgets (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    dashboard_id          uuid NOT NULL REFERENCES reporting.dashboards(id) ON DELETE CASCADE,
    widget_type           varchar(80) NOT NULL,
    title                 varchar(180),
    source_resource_type  varchar(80),
    source_resource_id    uuid,
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    layout_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version bigint NOT NULL DEFAULT 1,
    deleted_by uuid REFERENCES identity.users(id),
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id),
    CONSTRAINT ck_reporting_dashboard_widgets_source_resource_type CHECK (source_resource_type IS NULL OR source_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')),
    CONSTRAINT fk_reporting_dashboard_widgets_workspace_dashboard FOREIGN KEY (workspace_id, dashboard_id) REFERENCES reporting.dashboards(workspace_id, id));
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_active ON reporting.dashboard_widgets(dashboard_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_workspace_dashboard ON reporting.dashboard_widgets(workspace_id, dashboard_id);

CREATE TRIGGER trg_reporting_dashboard_widgets_updated_at
BEFORE UPDATE ON reporting.dashboard_widgets
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS reporting.reporting_snapshots (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    snapshot_type         varchar(120) NOT NULL,
    resource_type         varchar(80),
    resource_id           uuid,
    data_json             jsonb NOT NULL,
    calculated_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_reporting_snapshots_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);

CREATE INDEX IF NOT EXISTS ix_reporting_snapshots_workspace_type_time ON reporting.reporting_snapshots(workspace_id, snapshot_type, calculated_at DESC);


-- =============================================================================
-- 11. search schema
-- Service candidate: Search Service
-- =============================================================================

CREATE TABLE IF NOT EXISTS search.search_documents (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    title                 text NOT NULL,
    content               text,
    tags                  text[] NOT NULL DEFAULT ARRAY[]::text[],
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    -- tsvector for full-text search (auto-updated via trigger)
    search_vector         tsvector,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ux_search_documents_resource UNIQUE(workspace_id, resource_type, resource_id),
    CONSTRAINT ck_search_documents_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_search_search_documents_updated_at
BEFORE UPDATE ON search.search_documents
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE INDEX IF NOT EXISTS ix_search_documents_workspace_type ON search.search_documents(workspace_id, resource_type);
CREATE INDEX IF NOT EXISTS ix_search_documents_title_trgm ON search.search_documents USING gin(title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_search_documents_content_trgm ON search.search_documents USING gin(content gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_search_documents_tags ON search.search_documents USING gin(tags);
CREATE INDEX IF NOT EXISTS ix_search_documents_search_vector ON search.search_documents USING gin(search_vector);

CREATE OR REPLACE FUNCTION search.update_search_vector()
RETURNS trigger AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('simple', coalesce(NEW.title, '')), 'A') ||
        setweight(to_tsvector('simple', coalesce(NEW.content, '')), 'B') ||
        setweight(to_tsvector('simple', array_to_string(coalesce(NEW.tags, ARRAY[]::text[]), ' ')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_search_documents_search_vector
BEFORE INSERT OR UPDATE ON search.search_documents
FOR EACH ROW EXECUTE FUNCTION search.update_search_vector();

CREATE TABLE IF NOT EXISTS search.search_index_jobs (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type         varchar(80) NOT NULL,
    resource_id           uuid NOT NULL,
    operation             varchar(40) NOT NULL CHECK (operation IN ('Upsert', 'Delete', 'Reindex', 'Rebuild')),
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Running', 'Succeeded', 'Failed', 'Cancelled')),
    priority              integer NOT NULL DEFAULT 100,
    attempt_count         integer NOT NULL DEFAULT 0,
    max_attempts          integer NOT NULL DEFAULT 5,
    available_at          timestamptz NOT NULL DEFAULT now(),
    locked_by             varchar(120),
    locked_until          timestamptz,
    correlation_id        uuid,
    causation_id          uuid,
    error_message         text,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    processed_at          timestamptz,
    CONSTRAINT ck_search_index_jobs_resource_type CHECK (resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_search_search_index_jobs_updated_at
BEFORE UPDATE ON search.search_index_jobs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE INDEX IF NOT EXISTS ix_search_index_jobs_pending
ON search.search_index_jobs(status, priority, available_at, created_at)
WHERE status IN ('Pending', 'Failed');

CREATE INDEX IF NOT EXISTS ix_search_index_jobs_locks
ON search.search_index_jobs(locked_until)
WHERE status = 'Running';

CREATE INDEX IF NOT EXISTS ix_search_index_jobs_resource
ON search.search_index_jobs(workspace_id, resource_type, resource_id, created_at DESC);



-- =============================================================================
-- 12. ops schema
-- Owns: idempotency, import/export, job locks, cross-cutting operations
-- =============================================================================

CREATE TABLE IF NOT EXISTS ops.idempotency_keys (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid REFERENCES identity.users(id) ON DELETE CASCADE,
    scope                 varchar(240) NOT NULL,
    idempotency_key       varchar(200) NOT NULL,
    request_method        varchar(20) NOT NULL,
    request_path          text NOT NULL,
    request_hash          text NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Started'
                          CHECK (status IN ('Started', 'Completed', 'Failed', 'Expired')),
    response_status_code  integer,
    response_body_json    jsonb,
    error_message         text,
    locked_until          timestamptz,
    expires_at            timestamptz NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    completed_at          timestamptz,
    CONSTRAINT ck_ops_idempotency_keys_scope_not_blank CHECK (btrim(scope) <> ''),
    CONSTRAINT ck_ops_idempotency_keys_key_not_blank CHECK (btrim(idempotency_key) <> '')
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ops_idempotency_keys_scope_key
ON ops.idempotency_keys(scope, idempotency_key);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_expires_at
ON ops.idempotency_keys(expires_at);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_workspace_status
ON ops.idempotency_keys(workspace_id, status, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_user_status
ON ops.idempotency_keys(user_id, status, created_at DESC)
WHERE user_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS ops.processed_events (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id              uuid NOT NULL,
    consumer_name         varchar(200) NOT NULL,
    message_type          varchar(240) NOT NULL,
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    aggregate_type        varchar(160),
    aggregate_id          uuid,
    correlation_id        uuid,
    causation_id          uuid,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    processed_at          timestamptz NOT NULL DEFAULT now(),
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_ops_processed_events_event_consumer UNIQUE(event_id, consumer_name)
);
CREATE INDEX IF NOT EXISTS ix_ops_processed_events_consumer_time
ON ops.processed_events(consumer_name, processed_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_processed_events_workspace_time
ON ops.processed_events(workspace_id, processed_at DESC)
WHERE workspace_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS ops.import_jobs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type              varchar(80) NOT NULL,
    target_resource_type  varchar(80),
    target_resource_id    uuid,
    source_file_attachment_id uuid REFERENCES collab.attachments(id) ON DELETE SET NULL,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Running', 'Succeeded', 'CompletedWithErrors', 'Failed', 'Cancelled', 'Expired')),
    total_records         integer NOT NULL DEFAULT 0 CHECK (total_records >= 0),
    processed_records     integer NOT NULL DEFAULT 0 CHECK (processed_records >= 0),
    succeeded_records     integer NOT NULL DEFAULT 0 CHECK (succeeded_records >= 0),
    failed_records        integer NOT NULL DEFAULT 0 CHECK (failed_records >= 0),
    options_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    result_json           jsonb,
    error_summary         text,
    error_message         text,
    error_file_attachment_id uuid REFERENCES collab.attachments(id) ON DELETE SET NULL,
    requested_by_user_id  uuid REFERENCES identity.users(id),
    started_at            timestamptz,
    completed_at          timestamptz,
    cancelled_at          timestamptz,
    expires_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ck_ops_import_jobs_progress CHECK (processed_records <= total_records OR total_records = 0),
    CONSTRAINT ck_ops_import_jobs_target_resource_type CHECK (target_resource_type IS NULL OR target_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_ops_import_jobs_updated_at
BEFORE UPDATE ON ops.import_jobs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_workspace_status
ON ops.import_jobs(workspace_id, status, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_expiry
ON ops.import_jobs(expires_at)
WHERE expires_at IS NOT NULL;

CREATE TABLE IF NOT EXISTS ops.export_jobs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type              varchar(80) NOT NULL,
    source_resource_type  varchar(80),
    source_resource_id    uuid,
    status                varchar(40) NOT NULL DEFAULT 'Pending'
                          CHECK (status IN ('Pending', 'Running', 'Succeeded', 'Failed', 'Cancelled', 'Expired')),
    format                varchar(40) NOT NULL DEFAULT 'Csv' CHECK (format IN ('Csv', 'Excel', 'Pdf', 'Json')),
    row_count             integer CHECK (row_count IS NULL OR row_count >= 0),
    options_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    filters_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    result_attachment_id  uuid REFERENCES collab.attachments(id) ON DELETE SET NULL,
    result_file_id        uuid,
    storage_provider      varchar(80),
    storage_key           text,
    download_url          text,
    expires_at            timestamptz,
    error_message         text,
    requested_by_user_id  uuid REFERENCES identity.users(id),
    started_at            timestamptz,
    completed_at          timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ck_ops_export_jobs_source_resource_type CHECK (source_resource_type IS NULL OR source_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_ops_export_jobs_updated_at
BEFORE UPDATE ON ops.export_jobs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_workspace_status
ON ops.export_jobs(workspace_id, status, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_expiry
ON ops.export_jobs(expires_at)
WHERE expires_at IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_source_resource
ON ops.export_jobs(workspace_id, source_resource_type, source_resource_id, created_at DESC)
WHERE source_resource_type IS NOT NULL AND source_resource_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS ops.job_locks (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    lock_key              varchar(240) NOT NULL UNIQUE,
    locked_by             varchar(120) NOT NULL,
    fencing_token         bigint NOT NULL DEFAULT 1,
    locked_until          timestamptz NOT NULL,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    acquired_at           timestamptz NOT NULL DEFAULT now(),
    renewed_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    CONSTRAINT ck_ops_job_locks_fencing_token_positive CHECK (fencing_token > 0)
);
CREATE TRIGGER trg_ops_job_locks_updated_at
BEFORE UPDATE ON ops.job_locks
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_ops_job_locks_locked_until ON ops.job_locks(locked_until);
CREATE INDEX IF NOT EXISTS ix_ops_job_locks_owner ON ops.job_locks(locked_by, locked_until);


-- =============================================================================
-- 13. Templates in work schema
-- =============================================================================

CREATE TABLE IF NOT EXISTS work.board_templates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    description           text,
    category              varchar(120),
    icon                  varchar(80),
    is_system             boolean NOT NULL DEFAULT false,
    is_public             boolean NOT NULL DEFAULT false,
    schema_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    default_views_json    jsonb NOT NULL DEFAULT '[]'::jsonb,
    default_groups_json   jsonb NOT NULL DEFAULT '[]'::jsonb,
    sample_items_json     jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_work_board_templates_updated_at
BEFORE UPDATE ON work.board_templates
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();


CREATE TABLE IF NOT EXISTS work.item_templates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    description           text,
    values_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    checklist_json        jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    CONSTRAINT ux_work_item_templates_board_name UNIQUE(board_id, name),
    deleted_at timestamptz,
    deleted_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    delete_reason text,
    restored_at timestamptz,
    restored_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_work_item_templates_updated_at
BEFORE UPDATE ON work.item_templates
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();



-- =============================================================================
-- 14. Seed base billing plans
-- =============================================================================

INSERT INTO billing.plans (code, name, description, price_cents, currency, billing_period)
VALUES
    ('free', 'Free', 'Free workspace plan', 0, 'USD', 'Free'),
    ('pro', 'Pro', 'Pro workspace plan', 1200, 'USD', 'Monthly'),
    ('enterprise', 'Enterprise', 'Enterprise workspace plan', 0, 'USD', 'Monthly')
ON CONFLICT (code) DO NOTHING;

INSERT INTO billing.plan_limits (plan_id, feature_code, limit_value, is_enabled)
SELECT p.id, 'max_members', 5, true
FROM billing.plans p WHERE p.code = 'free'
ON CONFLICT (plan_id, feature_code) DO NOTHING;

INSERT INTO billing.plan_limits (plan_id, feature_code, limit_value, is_enabled)
SELECT p.id, 'max_boards', 10, true
FROM billing.plans p WHERE p.code = 'free'
ON CONFLICT (plan_id, feature_code) DO NOTHING;

INSERT INTO billing.plan_limits (plan_id, feature_code, limit_value, is_enabled)
SELECT p.id, 'automation', 0, false
FROM billing.plans p WHERE p.code = 'free'
ON CONFLICT (plan_id, feature_code) DO NOTHING;


-- =============================================================================
-- 15. Compatibility views for legacy table names
-- =============================================================================

CREATE OR REPLACE VIEW work.v_legacy_lists AS
SELECT id, board_id, name, color, position, status, created_at, updated_at
FROM work.board_groups;

CREATE OR REPLACE VIEW work.v_legacy_cards AS
SELECT
    id,
    board_id,
    group_id AS list_id,
    name AS title,
    description_markdown AS description,
    values_json,
    position,
    is_archived,
    (deleted_at IS NOT NULL) AS is_deleted,
    created_at,
    updated_at
FROM work.board_items;

CREATE OR REPLACE VIEW work.v_legacy_board_columns AS
SELECT
    id,
    board_id,
    key,
    name,
    field_type AS column_type,
    settings_json,
    position,
    is_required,
    is_system,
    (deleted_at IS NOT NULL) AS is_deleted,
    created_at,
    updated_at
FROM work.board_fields;

CREATE OR REPLACE VIEW governance.v_legacy_permissions AS
SELECT
    id,
    workspace_id,
    resource_type,
    resource_id,
    subject_type,
    subject_id,
    level,
    granted_by_user_id,
    granted_at,
    expires_at,
    is_revoked,
    revoked_at,
    created_at,
    updated_at
FROM governance.resource_permissions;



-- =============================================================================
-- 17. TTL Cleanup — scheduled maintenance for append-only tables
-- =============================================================================
--
-- These functions are called by automation.scheduled_jobs (cron) or pg_cron.
-- Retention periods should match governance.audit_retention_policies.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.cleanup_expired_sessions() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM identity.user_sessions
    WHERE expires_at < now() - interval '7 days'
      AND status IN ('Expired', 'Revoked');
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION ops.cleanup_expired_idempotency_keys() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM ops.idempotency_keys WHERE expires_at < now();
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION ops.cleanup_old_login_attempts() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM identity.user_login_attempts WHERE occurred_at < now() - interval '90 days';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION ops.cleanup_processed_search_index_jobs() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM search.search_index_jobs
    WHERE status IN ('Succeeded', 'Failed', 'Cancelled')
      AND created_at < now() - interval '30 days';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION ops.cleanup_old_processed_events() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM ops.processed_events
    WHERE processed_at < now() - interval '90 days';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION ops.cleanup_completed_outbox_messages() RETURNS integer AS $$
DECLARE deleted_count integer;
BEGIN
    DELETE FROM automation.outbox_messages
    WHERE status = 'Processed'
      AND processed_at < now() - interval '7 days';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Seed TTL cleanup jobs into scheduled_jobs (runs nightly at 02:00 UTC)
INSERT INTO automation.scheduled_jobs (job_type, schedule_kind, cron_expression, timezone, payload_json, status)
VALUES
    ('cleanup_expired_sessions',        'Cron', '0 2 * * *', 'UTC', '{}'::jsonb, 'Active'),
    ('cleanup_expired_idempotency_keys','Cron', '0 2 * * *', 'UTC', '{}'::jsonb, 'Active'),
    ('cleanup_old_login_attempts',      'Cron', '0 3 * * 0', 'UTC', '{}'::jsonb, 'Active'),
    ('cleanup_processed_search_jobs',   'Cron', '0 3 * * *', 'UTC', '{}'::jsonb, 'Active'),
    ('cleanup_old_processed_events',    'Cron', '0 3 * * 0', 'UTC', '{}'::jsonb, 'Active'),
    ('cleanup_completed_outbox',        'Cron', '0 4 * * *', 'UTC', '{}'::jsonb, 'Active')
ON CONFLICT DO NOTHING;


-- =============================================================================
-- 18. Cross-schema FK — service-split migration guide
-- =============================================================================
--
-- This schema currently uses PostgreSQL FKs across schema boundaries
-- (e.g. work.* -> identity.users, work.* -> workspace.workspaces).
-- These are intentional for the Modular Monolith phase and provide strong
-- referential integrity at zero application cost.
--
-- When splitting into microservices, follow this migration path per FK:
--
--   Phase 1 (Monolith): FK enforced by PostgreSQL — current state.
--   Phase 2 (Pre-split): Remove FK constraint, add application-layer check
--                        + event-driven consistency via automation.outbox_messages.
--   Phase 3 (Split):     Each service owns its DB. Cross-service references
--                        become IDs only. Use Saga/choreography for consistency.
--
-- High-impact FKs to address first when splitting:
--   work.*       -> identity.users   (29 FKs) — replace with user projection table
--   work.*       -> workspace.*      (25 FKs) — replace with workspace snapshot
--   governance.* -> identity/workspace (28 FKs) — move to event-sourced permissions
--   collab.*     -> identity/workspace (24 FKs) — notification service owns user refs
--
-- Do NOT remove FKs prematurely — they catch data bugs for free in monolith phase.
-- =============================================================================


-- =============================================================================
-- End of schema
-- =============================================================================


-- =============================================================================
-- SECTION 2: SOFT DELETE COMPLETION PATCH
-- =============================================================================

-- =============================================================================
-- Notrelix Enterprise Soft Delete Migration Patch
-- Target: notrelix-schema-v2.sql
-- Purpose:
--   Move user-facing/business resources toward soft delete by default.
--
-- Important:
--   This patch is additive and safe to run after v2 schema.
--   It does not remove FK constraints because existing FK constraint names are
--   generated by migrations/EF. FK delete behavior should be updated in EF
--   configurations and generated as a controlled migration.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 01. Helper comments
-- -----------------------------------------------------------------------------
-- Soft delete policy:
--   - User-facing aggregate roots and user-visible child resources use deleted_at.
--   - Access queries must filter deleted_at IS NULL unless explicitly querying trash/archive.
--   - Hard delete is reserved for TTL/security/ephemeral/technical data:
--       sessions, password reset tokens, email verification tokens,
--       idempotency keys, outbox processed history after retention,
--       search index jobs, webhook delivery history after retention.
--   - Physical purge must be an explicit admin/retention workflow, never normal DELETE.













-- -----------------------------------------------------------------------------
-- 03. Partial indexes for active rows
-- -----------------------------------------------------------------------------

CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_active_lookup
ON governance.resource_permissions(workspace_id, resource_type, resource_id)
WHERE deleted_at IS NULL AND is_revoked = false;

CREATE INDEX IF NOT EXISTS ix_governance_share_links_active_resource
ON governance.share_links(workspace_id, resource_type, resource_id)
WHERE deleted_at IS NULL AND status = 'Enabled';

CREATE INDEX IF NOT EXISTS ix_work_labels_active_board_name
ON work.labels(workspace_id, board_id, name)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_work_checklists_active_item_position
ON work.checklists(workspace_id, board_id, item_id, position)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_work_checklist_items_active_checklist_position
ON work.checklist_items(workspace_id, checklist_id, position)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_work_board_templates_active_workspace_category
ON work.board_templates(workspace_id, category)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_work_item_templates_active_board_name
ON work.item_templates(board_id, name)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_docs_page_templates_active_workspace_category
ON docs.page_templates(workspace_id, category)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_docs_resource_links_active_source
ON docs.resource_links(workspace_id, source_resource_type, source_resource_id)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_docs_resource_links_active_target
ON docs.resource_links(workspace_id, target_resource_type, target_resource_id)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_collab_resource_watchers_active_user
ON collab.resource_watchers(workspace_id, user_id)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_automation_templates_active_workspace_category
ON automation.automation_templates(workspace_id, category)
WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_active_workspace
ON integration.calendar_integrations(workspace_id, status)
WHERE deleted_at IS NULL;

-- -----------------------------------------------------------------------------
-- 04. Optional active views
-- -----------------------------------------------------------------------------
-- These views are useful while refactoring read queries.
-- Application should still prefer explicit filters/read services.

CREATE OR REPLACE VIEW work.active_boards AS
SELECT * FROM work.boards
WHERE deleted_at IS NULL AND status <> 'Deleted';

CREATE OR REPLACE VIEW work.active_board_items AS
SELECT * FROM work.board_items
WHERE deleted_at IS NULL;

CREATE OR REPLACE VIEW docs.active_pages AS
SELECT * FROM docs.pages
WHERE deleted_at IS NULL AND status <> 'Deleted';

CREATE OR REPLACE VIEW collab.active_comments AS
SELECT * FROM collab.comments
WHERE deleted_at IS NULL AND status <> 'Deleted';

CREATE OR REPLACE VIEW reporting.active_dashboards AS
SELECT * FROM reporting.dashboards
WHERE deleted_at IS NULL AND status <> 'Deleted';

-- -----------------------------------------------------------------------------
-- 05. EF Core delete behavior to apply in code, not directly here
-- -----------------------------------------------------------------------------
-- For business resources:
--   .OnDelete(DeleteBehavior.Restrict) or .OnDelete(DeleteBehavior.NoAction)
--
-- For optional audit/display references:
--   .OnDelete(DeleteBehavior.SetNull)
--
-- For truly owned technical/token data:
--   DeleteBehavior.Cascade is acceptable during controlled physical purge.
--
-- Normal user/API delete use cases must issue UPDATE ... SET deleted_at = now(),
-- never DELETE FROM business tables.
-- =============================================================================

-- =============================================================================
-- End of Notrelix Domain Complete Soft-Delete Schema
-- =============================================================================

-- =============================================================================
-- V3 ENTERPRISE DOMAIN ALIGNMENT PATCH
-- =============================================================================
--
-- This patch aligns the database with the clean Domain model:
--   - Domain excludes Search/Ops/Outbox/JobLocks/Idempotency as core domain.
--   - Business resources should use soft delete, not physical DELETE.
--   - Physical DELETE is blocked by default for business tables.
--   - Retention/purge jobs may opt-in by setting app.allow_hard_delete = 'on'
--     in a controlled transaction.
--
-- Usage for controlled purge only:
--   BEGIN;
--   SET LOCAL app.allow_hard_delete = 'on';
--   DELETE FROM work.board_items WHERE deleted_at < now() - interval '365 days';
--   COMMIT;
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.is_hard_delete_allowed()
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(current_setting('app.allow_hard_delete', true), 'off') IN ('on', 'true', '1');
$$;

CREATE OR REPLACE FUNCTION ops.prevent_business_hard_delete()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT ops.is_hard_delete_allowed() THEN
        RAISE EXCEPTION
            'Hard delete is blocked for business table %.%. Use soft delete or run controlled purge with app.allow_hard_delete=on.',
            TG_TABLE_SCHEMA,
            TG_TABLE_NAME
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN OLD;
END;
$$;

DO $$
DECLARE
    r record;
    trigger_name text;
BEGIN
    FOR r IN
        SELECT schema_name, table_name
        FROM (VALUES
        ('workspace','workspaces'),
        ('workspace','workspace_members'),
        ('workspace','workspace_invitations'),
        ('workspace','spaces'),
        ('workspace','teams'),
        ('workspace','team_members'),
        ('governance','resource_permissions'),
        ('governance','field_permissions'),
        ('governance','share_links'),
        ('governance','custom_roles'),
        ('governance','custom_role_permissions'),
        ('governance','workspace_member_role_assignments'),
        ('governance','permission_templates'),
        ('governance','workspace_policies'),
        ('work','boards'),
        ('work','board_groups'),
        ('work','board_fields'),
        ('work','field_options'),
        ('work','board_views'),
        ('work','board_items'),
        ('work','board_item_connections'),
        ('work','board_item_values'),
        ('work','board_item_members'),
        ('work','labels'),
        ('work','board_item_labels'),
        ('work','board_item_links'),
        ('work','checklists'),
        ('work','checklist_items'),
        ('work','board_view_user_preferences'),
        ('work','saved_filters'),
        ('work','relation_field_configs'),
        ('work','formula_dependencies'),
        ('work','rollup_snapshots'),
        ('work','approval_requests'),
        ('work','approval_steps'),
        ('work','workload_allocations'),
        ('work','board_templates'),
        ('work','item_templates'),
        ('docs','pages'),
        ('docs','blocks'),
        ('docs','document_versions'),
        ('docs','resource_links'),
        ('docs','page_templates'),
        ('collab','comments'),
        ('collab','reactions'),
        ('collab','mentions'),
        ('collab','notifications'),
        ('collab','notification_preferences'),
        ('collab','notification_deliveries'),
        ('collab','activity_logs'),
        ('collab','attachments'),
        ('collab','resource_watchers'),
        ('automation','automation_rules'),
        ('automation','automation_executions'),
        ('automation','scheduled_jobs'),
        ('automation','automation_templates'),
        ('integration','integration_connections'),
        ('integration','integration_scopes'),
        ('integration','integration_secret_versions'),
        ('integration','webhook_subscriptions'),
        ('integration','webhook_deliveries'),
        ('integration','inbound_webhook_events'),
        ('integration','calendar_integrations'),
        ('integration','calendar_event_links'),
        ('integration','integration_sync_cursors'),
        ('billing','plans'),
        ('billing','plan_limits'),
        ('billing','subscriptions'),
        ('billing','payment_methods'),
        ('billing','invoices'),
        ('billing','billing_events'),
        ('billing','usage_metrics'),
        ('billing','usage_metric_history'),
        ('billing','entitlements'),
        ('reporting','dashboards'),
        ('reporting','dashboard_widgets'),
        ('reporting','reporting_snapshots')
        ) AS t(schema_name, table_name)
    LOOP
        IF EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = r.schema_name
              AND table_name = r.table_name
        ) THEN
            trigger_name := 'trg_prevent_hard_delete_' || r.table_name;

            EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I;', trigger_name, r.schema_name, r.table_name);

            EXECUTE format(
                'CREATE TRIGGER %I BEFORE DELETE ON %I.%I FOR EACH ROW EXECUTE FUNCTION ops.prevent_business_hard_delete();',
                trigger_name,
                r.schema_name,
                r.table_name
            );
        END IF;
    END LOOP;
END $$;

COMMENT ON SCHEMA search IS
'Infrastructure/Search projection schema. search_documents and search_index_jobs are not core Domain aggregates.';

COMMENT ON SCHEMA ops IS
'Infrastructure/Operations schema. Idempotency keys, import/export jobs and job locks are technical/application concerns, not core Domain aggregates.';

COMMENT ON TABLE automation.outbox_messages IS
'Infrastructure Outbox table. OutboxMessage is not a Domain entity; Domain only raises IDomainEvent.';

COMMENT ON TABLE governance.resource_permission_inheritance_cache IS
'Permission projection/cache table. Not a core Governance aggregate. Rebuildable from source permissions and policies.';

COMMENT ON TABLE search.search_documents IS
'Search projection table. Rebuildable from source resources via indexing jobs/outbox events.';

COMMENT ON TABLE search.search_index_jobs IS
'Technical indexing queue. Not a Domain aggregate.';

COMMENT ON TABLE ops.idempotency_keys IS
'Idempotency storage for API/Application. Not a Domain aggregate.';

COMMENT ON TABLE ops.job_locks IS
'Background worker lock storage. Not a Domain aggregate.';

-- =============================================================================
-- SECTION 3: V4 MONDAY-STYLE SAAS HARDENING PATCH
-- =============================================================================
-- Version: 4.0
-- Generated by ChatGPT for Notrelix domain-harmonization review.
-- This section is additive. It can be appended to the V3 baseline to create a
-- complete install script for a new database, or copied out as a migration patch
-- after reviewing data consistency on an existing database.
--
-- Design goals:
--   1. Strong tenant isolation for a Monday-like multi-workspace SaaS.
--   2. Optimistic concurrency for realtime collaborative editing.
--   3. Rich account/workspace/board/field/dashboard permission model.
--   4. Board-to-board relations, mirror snapshots, WorkForms, dependencies,
--      time tracking, view pins and board subscriptions.
--   5. Quota/entitlement usage snapshots for billing limits.
--   6. Better outbox/integration contract and idempotency support.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- V4.01 Helper functions
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION ops.current_user_id() RETURNS uuid AS $$
BEGIN
    RETURN current_setting('app.current_user_id', true)::uuid;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;


CREATE OR REPLACE FUNCTION ops.current_workspace_id() RETURNS uuid AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_workspace_id', true), '')::uuid;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;


CREATE OR REPLACE FUNCTION ops.current_correlation_id() RETURNS text AS $$
BEGIN
    RETURN current_setting('app.correlation_id', true);
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION ops.increment_version()
RETURNS trigger AS $$
BEGIN
    NEW.version = COALESCE(OLD.version, 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- -----------------------------------------------------------------------------
-- V4.03 Soft-delete metadata standardization
-- -----------------------------------------------------------------------------













































































-- -----------------------------------------------------------------------------
-- V4.04 Monday-style hierarchy and board item hardening
-- -----------------------------------------------------------------------------















CREATE UNIQUE INDEX IF NOT EXISTS ux_work_board_items_board_item_key_active
ON work.board_items(board_id, item_key)
WHERE item_key IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_items_parent ON work.board_items(workspace_id, board_id, parent_item_id);
CREATE INDEX IF NOT EXISTS ix_work_board_items_due_at ON work.board_items(workspace_id, due_at) WHERE deleted_at IS NULL;







-- Board subscribers/owners. Keep this separate from ACL rules because product UI often needs fast lists.
CREATE TABLE IF NOT EXISTS work.board_subscribers (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    subscriber_role       varchar(40) NOT NULL DEFAULT 'Subscriber'
                          CHECK (subscriber_role IN ('Owner','Subscriber','Guest')),
    notification_json     jsonb NOT NULL DEFAULT '{}'::jsonb,
    subscribed_at         timestamptz NOT NULL DEFAULT now(),
    subscribed_by         uuid REFERENCES identity.users(id),
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_board_subscribers_board_user UNIQUE(board_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_work_board_subscribers_user ON work.board_subscribers(workspace_id, user_id);

-- Pinned views: supports multiple pinned board views globally or per user.
CREATE TABLE IF NOT EXISTS work.board_view_pins (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    board_view_id         uuid NOT NULL REFERENCES work.board_views(id) ON DELETE CASCADE,
    user_id               uuid REFERENCES identity.users(id) ON DELETE CASCADE,
    pin_scope             varchar(40) NOT NULL DEFAULT 'User'
                          CHECK (pin_scope IN ('User','BoardDefault')),
    position              numeric(20, 8) NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    CONSTRAINT ux_work_board_view_pins_scope UNIQUE(board_view_id, user_id, pin_scope)
);
CREATE INDEX IF NOT EXISTS ix_work_board_view_pins_board ON work.board_view_pins(workspace_id, board_id, position);

-- -----------------------------------------------------------------------------
-- V4.05 Connect Boards / Mirror / dependency / time tracking support
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS work.board_relations (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    source_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    target_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    source_field_id       uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    target_field_id       uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    relation_type         varchar(60) NOT NULL DEFAULT 'ConnectBoards'
                          CHECK (relation_type IN ('ConnectBoards','Mirror','Rollup','Dependency','Hierarchy')),
    direction             varchar(40) NOT NULL DEFAULT 'TwoWay'
                          CHECK (direction IN ('OneWay','TwoWay')),
    sync_mode             varchar(40) NOT NULL DEFAULT 'Manual'
                          CHECK (sync_mode IN ('Manual','Realtime','Scheduled')),
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active','Paused','Broken','Deleted')),
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_board_relations_unique UNIQUE(workspace_id, source_board_id, target_board_id, relation_type, source_field_id)
);
CREATE TRIGGER trg_work_board_relations_updated_at
BEFORE UPDATE ON work.board_relations
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_board_relations_source ON work.board_relations(workspace_id, source_board_id, status);
CREATE INDEX IF NOT EXISTS ix_work_board_relations_target ON work.board_relations(workspace_id, target_board_id, status);

CREATE TABLE IF NOT EXISTS work.board_item_connections (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    relation_id           uuid NOT NULL REFERENCES work.board_relations(id) ON DELETE CASCADE,
    source_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    source_item_id        uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    target_board_id       uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    target_item_id        uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    sync_status           varchar(40) NOT NULL DEFAULT 'InSync'
                          CHECK (sync_status IN ('InSync','Pending','Conflict','Failed')),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    CONSTRAINT ux_work_board_item_connections_unique UNIQUE(relation_id, source_item_id, target_item_id),
    version bigint NOT NULL DEFAULT 1);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_source ON work.board_item_connections(workspace_id, source_board_id, source_item_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_target ON work.board_item_connections(workspace_id, target_board_id, target_item_id);

CREATE TABLE IF NOT EXISTS work.mirror_value_snapshots (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    relation_id           uuid NOT NULL REFERENCES work.board_relations(id) ON DELETE CASCADE,
    connection_id         uuid NOT NULL REFERENCES work.board_item_connections(id) ON DELETE CASCADE,
    source_field_id       uuid NOT NULL REFERENCES work.board_fields(id) ON DELETE CASCADE,
    mirrored_field_id     uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    value_json            jsonb,
    value_hash            varchar(128),
    is_stale              boolean NOT NULL DEFAULT false,
    computed_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_work_mirror_value_snapshots UNIQUE(connection_id, source_field_id, mirrored_field_id)
);
CREATE INDEX IF NOT EXISTS ix_work_mirror_value_snapshots_relation ON work.mirror_value_snapshots(workspace_id, relation_id, is_stale);

CREATE TABLE IF NOT EXISTS work.item_dependencies (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    predecessor_item_id   uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    successor_item_id     uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    dependency_type       varchar(40) NOT NULL DEFAULT 'FinishToStart'
                          CHECK (dependency_type IN ('FinishToStart','StartToStart','FinishToFinish','StartToFinish')),
    lag_minutes           integer NOT NULL DEFAULT 0,
    status                varchar(40) NOT NULL DEFAULT 'Active'
                          CHECK (status IN ('Active','Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_work_item_dependencies_not_self CHECK (predecessor_item_id <> successor_item_id),
    CONSTRAINT ux_work_item_dependencies_unique UNIQUE(predecessor_item_id, successor_item_id, dependency_type)
);
CREATE TRIGGER trg_work_item_dependencies_updated_at
BEFORE UPDATE ON work.item_dependencies
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_item_dependencies_successor ON work.item_dependencies(workspace_id, successor_item_id);

CREATE TABLE IF NOT EXISTS work.time_tracking_entries (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    item_id               uuid NOT NULL REFERENCES work.board_items(id) ON DELETE CASCADE,
    user_id               uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    started_at            timestamptz NOT NULL,
    ended_at              timestamptz,
    duration_seconds      integer GENERATED ALWAYS AS (
                              CASE WHEN ended_at IS NULL THEN NULL ELSE GREATEST(0, EXTRACT(EPOCH FROM ended_at - started_at)::integer) END
                          ) STORED,
    status                varchar(40) NOT NULL DEFAULT 'Running'
                          CHECK (status IN ('Running','Stopped','Deleted')),
    note                  text,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_work_time_tracking_entries_time CHECK (ended_at IS NULL OR ended_at >= started_at)
);
CREATE TRIGGER trg_work_time_tracking_entries_updated_at
BEFORE UPDATE ON work.time_tracking_entries
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_item ON work.time_tracking_entries(workspace_id, board_id, item_id, started_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_user ON work.time_tracking_entries(workspace_id, user_id, started_at DESC);

-- -----------------------------------------------------------------------------
-- V4.06 WorkForms / intake forms connected to boards
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS work.forms (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    slug                  varchar(220) NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Draft'
                          CHECK (status IN ('Draft','Published','Closed','Deleted')),
    visibility            varchar(40) NOT NULL DEFAULT 'PublicLink'
                          CHECK (visibility IN ('Private','Workspace','PublicLink')),
    settings_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    submitter_policy_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_forms_workspace_slug UNIQUE(workspace_id, slug),
    CONSTRAINT ux_work_forms_workspace_board_id_id UNIQUE(workspace_id, board_id, id),
    CONSTRAINT fk_work_forms_workspace_board FOREIGN KEY (workspace_id, board_id) REFERENCES work.boards(workspace_id, id));
CREATE TRIGGER trg_work_forms_updated_at
BEFORE UPDATE ON work.forms
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_forms_board_status ON work.forms(workspace_id, board_id, status);

CREATE TABLE IF NOT EXISTS work.form_questions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    form_id               uuid NOT NULL REFERENCES work.forms(id) ON DELETE CASCADE,
    board_field_id        uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    question_key          varchar(120) NOT NULL,
    label                 varchar(240) NOT NULL,
    question_type         varchar(60) NOT NULL,
    is_required           boolean NOT NULL DEFAULT false,
    position              numeric(20,8) NOT NULL DEFAULT 0,
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_work_form_questions_key UNIQUE(form_id, question_key),
    CONSTRAINT fk_work_form_questions_workspace_form FOREIGN KEY (workspace_id, form_id) REFERENCES work.forms(workspace_id, id));
CREATE TRIGGER trg_work_form_questions_updated_at
BEFORE UPDATE ON work.form_questions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_form_questions_form_position ON work.form_questions(form_id, position);
CREATE INDEX IF NOT EXISTS ix_work_form_questions_workspace_form_position ON work.form_questions(workspace_id, form_id, position);

CREATE TABLE IF NOT EXISTS work.form_submissions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    form_id               uuid NOT NULL REFERENCES work.forms(id) ON DELETE CASCADE,
    board_id              uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    created_item_id       uuid REFERENCES work.board_items(id) ON DELETE SET NULL,
    submitter_user_id     uuid REFERENCES identity.users(id),
    submitter_email       citext,
    payload_json          jsonb NOT NULL DEFAULT '{}'::jsonb,
    source_ip             inet,
    user_agent            text,
    status                varchar(40) NOT NULL DEFAULT 'Accepted'
                          CHECK (status IN ('Accepted','Rejected','Spam','Deleted')),
    submitted_at          timestamptz NOT NULL DEFAULT now(),
    processed_at          timestamptz,
    updated_at timestamptz,
    updated_by uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT fk_work_form_submissions_workspace_form FOREIGN KEY (workspace_id, board_id, form_id) REFERENCES work.forms(workspace_id, board_id, id),
    CONSTRAINT fk_work_form_submissions_workspace_item FOREIGN KEY (workspace_id, board_id, created_item_id) REFERENCES work.board_items(workspace_id, board_id, id));
CREATE TRIGGER trg_work_form_submissions_updated_at
BEFORE UPDATE ON work.form_submissions
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_form_time ON work.form_submissions(workspace_id, form_id, submitted_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_item ON work.form_submissions(workspace_id, created_item_id);

-- -----------------------------------------------------------------------------
-- V4.07 Permission engine hardening
-- -----------------------------------------------------------------------------









CREATE TABLE IF NOT EXISTS governance.permission_rules (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    scope_type            varchar(40) NOT NULL CHECK (scope_type IN ('Account','Workspace','Space','Board','BoardField','BoardItem','Dashboard','Doc','Automation','Integration','Billing')),
    resource_type         varchar(80),
    resource_id           uuid,
    subject_type          varchar(80) NOT NULL CHECK (subject_type IN ('User','WorkspaceRole','CustomRole','Team','Guest','PublicLink','ExternalEmail','ApiToken')),
    subject_id            uuid,
    subject_key           varchar(160),
    action                varchar(160) NOT NULL,
    effect                varchar(20) NOT NULL DEFAULT 'Allow' CHECK (effect IN ('Allow','Deny')),
    condition_json        jsonb NOT NULL DEFAULT '{}'::jsonb,
    priority              integer NOT NULL DEFAULT 100,
    starts_at             timestamptz,
    expires_at            timestamptz,
    status                varchar(40) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Deleted')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_governance_permission_rules_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE TRIGGER trg_governance_permission_rules_updated_at
BEFORE UPDATE ON governance.permission_rules
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_eval
ON governance.permission_rules(workspace_id, scope_type, resource_type, resource_id, action, priority)
WHERE status = 'Active' AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_subject
ON governance.permission_rules(workspace_id, subject_type, subject_id, subject_key)
WHERE status = 'Active' AND deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_governance_permission_rules_unique
ON governance.permission_rules(
    workspace_id,
    scope_type,
    COALESCE(resource_type, ''),
    COALESCE(resource_id, '00000000-0000-0000-0000-000000000000'::uuid),
    subject_type,
    COALESCE(subject_id, '00000000-0000-0000-0000-000000000000'::uuid),
    COALESCE(subject_key, ''),
    action
);

-- -----------------------------------------------------------------------------
-- V4.08 Enterprise identity/admin security
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS identity.sso_providers (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider_type         varchar(40) NOT NULL CHECK (provider_type IN ('SAML','OIDC')),
    name                  varchar(160) NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Enabled','Disabled','Deleted')),
    domain                citext,
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_identity_sso_providers_workspace_name UNIQUE(workspace_id, name)
);
CREATE TRIGGER trg_identity_sso_providers_updated_at
BEFORE UPDATE ON identity.sso_providers
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE TABLE IF NOT EXISTS identity.api_tokens (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id               uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    name                  varchar(180) NOT NULL,
    token_hash            text NOT NULL UNIQUE,
    scopes_json           jsonb NOT NULL DEFAULT '[]'::jsonb,
    status                varchar(40) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Revoked','Expired','Deleted')),
    last_used_at          timestamptz,
    expires_at            timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    revoked_at            timestamptz,
    revoked_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1
);
CREATE TRIGGER trg_identity_api_tokens_updated_at
BEFORE UPDATE ON identity.api_tokens
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_identity_api_tokens_workspace_user ON identity.api_tokens(workspace_id, user_id, status);

CREATE TABLE IF NOT EXISTS identity.scim_directory_syncs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider_name         varchar(120) NOT NULL,
    status                varchar(40) NOT NULL DEFAULT 'Enabled' CHECK (status IN ('Enabled','Paused','Disabled','Deleted')),
    last_sync_at          timestamptz,
    cursor_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    config_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_identity_scim_directory_syncs_workspace_provider UNIQUE(workspace_id, provider_name)
);
CREATE TRIGGER trg_identity_scim_directory_syncs_updated_at
BEFORE UPDATE ON identity.scim_directory_syncs
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

-- -----------------------------------------------------------------------------
-- V4.09 Billing quota snapshots and usage ledger
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS billing.workspace_feature_usage (
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code          varchar(120) NOT NULL,
    used_value            bigint NOT NULL DEFAULT 0 CHECK (used_value >= 0),
    reserved_value        bigint NOT NULL DEFAULT 0 CHECK (reserved_value >= 0),
    limit_value_snapshot  bigint,
    reset_period          varchar(40) NOT NULL DEFAULT 'None' CHECK (reset_period IN ('None','Daily','Monthly','Yearly')),
    reset_at              timestamptz,
    updated_at            timestamptz NOT NULL DEFAULT now(),
    updated_by            uuid REFERENCES identity.users(id),
    version               bigint NOT NULL DEFAULT 1,
    PRIMARY KEY(workspace_id, feature_code)
);
CREATE TRIGGER trg_billing_workspace_feature_usage_updated_at
BEFORE UPDATE ON billing.workspace_feature_usage
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_billing_workspace_feature_usage_reset ON billing.workspace_feature_usage(reset_at) WHERE reset_at IS NOT NULL;

CREATE TABLE IF NOT EXISTS billing.feature_usage_ledger (
    id                    uuid PRIMARY KEY,
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code          varchar(120) NOT NULL,
    delta_value           bigint NOT NULL,
    reason                varchar(160) NOT NULL,
    resource_type         varchar(80),
    resource_id           uuid,
    idempotency_key       varchar(200),
    metadata_json         jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at           timestamptz NOT NULL DEFAULT now(),
    actor_user_id         uuid REFERENCES identity.users(id),
    CONSTRAINT ux_billing_feature_usage_ledger_idempotency UNIQUE(workspace_id, feature_code, idempotency_key),
    CONSTRAINT ck_billing_feature_usage_ledger_resource_type CHECK (resource_type IS NULL OR resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External'))
);
CREATE INDEX IF NOT EXISTS ix_billing_feature_usage_ledger_workspace_time ON billing.feature_usage_ledger(workspace_id, occurred_at DESC);

CREATE OR REPLACE FUNCTION billing.apply_feature_usage_delta(
    p_workspace_id uuid,
    p_feature_code varchar,
    p_delta_value bigint,
    p_reason varchar,
    p_resource_type varchar DEFAULT NULL,
    p_resource_id uuid DEFAULT NULL,
    p_idempotency_key varchar DEFAULT NULL,
    p_actor_user_id uuid DEFAULT NULL
) RETURNS void AS $$
BEGIN
    IF p_idempotency_key IS NOT NULL AND EXISTS (
        SELECT 1 FROM billing.feature_usage_ledger
        WHERE workspace_id = p_workspace_id
          AND feature_code = p_feature_code
          AND idempotency_key = p_idempotency_key
    ) THEN
        RETURN;
    END IF;

    INSERT INTO billing.workspace_feature_usage(workspace_id, feature_code, used_value)
    VALUES (p_workspace_id, p_feature_code, GREATEST(0, p_delta_value))
    ON CONFLICT (workspace_id, feature_code) DO UPDATE
    SET used_value = GREATEST(0, billing.workspace_feature_usage.used_value + p_delta_value),
        updated_at = now();

    INSERT INTO billing.feature_usage_ledger(workspace_id, feature_code, delta_value, reason, resource_type, resource_id, idempotency_key, actor_user_id)
    VALUES (p_workspace_id, p_feature_code, p_delta_value, p_reason, p_resource_type, p_resource_id, p_idempotency_key, p_actor_user_id)
    ON CONFLICT DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- V4.10 Reporting dashboard sources
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS reporting.dashboard_sources (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    dashboard_id          uuid NOT NULL REFERENCES reporting.dashboards(id) ON DELETE CASCADE,
    source_type           varchar(40) NOT NULL DEFAULT 'Board' CHECK (source_type IN ('Board','BoardView','Search','External')),
    board_id              uuid REFERENCES work.boards(id) ON DELETE CASCADE,
    board_view_id         uuid REFERENCES work.board_views(id) ON DELETE SET NULL,
    filter_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_reporting_dashboard_sources_unique UNIQUE(dashboard_id, source_type, board_id, board_view_id)
);
CREATE TRIGGER trg_reporting_dashboard_sources_updated_at
BEFORE UPDATE ON reporting.dashboard_sources
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_sources_dashboard ON reporting.dashboard_sources(workspace_id, dashboard_id);

-- -----------------------------------------------------------------------------
-- V4.11 Integration / outbox contract hardening
-- -----------------------------------------------------------------------------







CREATE UNIQUE INDEX IF NOT EXISTS ux_automation_outbox_idempotency
ON automation.outbox_messages(workspace_id, idempotency_key)
WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_automation_outbox_pending_v4
ON automation.outbox_messages(status, next_attempt_at, created_at)
WHERE status IN ('Pending','Failed');
CREATE INDEX IF NOT EXISTS ix_automation_outbox_correlation ON automation.outbox_messages(correlation_id);



UPDATE integration.integration_scopes s
SET workspace_id = c.workspace_id
FROM integration.integration_connections c
WHERE s.connection_id = c.id AND s.workspace_id IS NULL;
UPDATE integration.integration_secret_versions s
SET workspace_id = c.workspace_id
FROM integration.integration_connections c
WHERE s.connection_id = c.id AND s.workspace_id IS NULL;
CREATE INDEX IF NOT EXISTS ix_integration_scopes_workspace_connection ON integration.integration_scopes(workspace_id, connection_id);
CREATE INDEX IF NOT EXISTS ix_integration_secret_versions_workspace_connection ON integration.integration_secret_versions(workspace_id, connection_id);

CREATE OR REPLACE FUNCTION integration.set_connection_workspace_id()
RETURNS trigger AS $$
BEGIN
    IF NEW.workspace_id IS NULL THEN
        SELECT workspace_id INTO NEW.workspace_id
        FROM integration.integration_connections
        WHERE id = NEW.connection_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_integration_scopes_set_workspace ON integration.integration_scopes;
CREATE TRIGGER trg_integration_scopes_set_workspace
BEFORE INSERT OR UPDATE ON integration.integration_scopes
FOR EACH ROW EXECUTE FUNCTION integration.set_connection_workspace_id();

DROP TRIGGER IF EXISTS trg_integration_secret_versions_set_workspace ON integration.integration_secret_versions;
CREATE TRIGGER trg_integration_secret_versions_set_workspace
BEFORE INSERT OR UPDATE ON integration.integration_secret_versions
FOR EACH ROW EXECUTE FUNCTION integration.set_connection_workspace_id();

-- -----------------------------------------------------------------------------
-- V4.12 Optional AI automation agents layer
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS automation.ai_agents (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name                  varchar(180) NOT NULL,
    description           text,
    scope_type            varchar(40) NOT NULL DEFAULT 'Workspace' CHECK (scope_type IN ('Workspace','Board','Doc','Dashboard')),
    scope_resource_id     uuid,
    status                varchar(40) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Enabled','Paused','Disabled','Deleted')),
    model_policy_json     jsonb NOT NULL DEFAULT '{}'::jsonb,
    instruction_json      jsonb NOT NULL DEFAULT '{}'::jsonb,
    tool_permissions_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid REFERENCES identity.users(id),
    updated_at            timestamptz,
    updated_by            uuid REFERENCES identity.users(id),
    deleted_at            timestamptz,
    version               bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_automation_ai_agents_workspace_name UNIQUE(workspace_id, name)
);
CREATE TRIGGER trg_automation_ai_agents_updated_at
BEFORE UPDATE ON automation.ai_agents
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_scope ON automation.ai_agents(workspace_id, scope_type, scope_resource_id, status);

CREATE TABLE IF NOT EXISTS automation.ai_agent_runs (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id          uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    ai_agent_id           uuid NOT NULL REFERENCES automation.ai_agents(id) ON DELETE CASCADE,
    trigger_type          varchar(80) NOT NULL,
    trigger_resource_type varchar(80),
    trigger_resource_id   uuid,
    status                varchar(40) NOT NULL DEFAULT 'Queued' CHECK (status IN ('Queued','Running','Succeeded','Failed','Cancelled')),
    input_json            jsonb NOT NULL DEFAULT '{}'::jsonb,
    output_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    error_json            jsonb,
    started_at            timestamptz,
    finished_at           timestamptz,
    created_at            timestamptz NOT NULL DEFAULT now(),
    actor_user_id         uuid REFERENCES identity.users(id),
    correlation_id        uuid,
    CONSTRAINT ck_automation_ai_agent_runs_trigger_resource_type CHECK (trigger_resource_type IS NULL OR trigger_resource_type IN ('Account', 'Workspace', 'WorkspaceMember', 'Team', 'Space', 'Board', 'BoardGroup', 'BoardField', 'BoardItem', 'BoardView', 'Form', 'FormSubmission', 'Checklist', 'ChecklistItem', 'ApprovalRequest', 'Page', 'Block', 'DocumentVersion', 'ResourceLink', 'Dashboard', 'DashboardWidget', 'AutomationRule', 'AutomationExecution', 'ScheduledJob', 'AiAgent', 'IntegrationConnection', 'CalendarIntegration', 'WebhookSubscription', 'Comment', 'Attachment', 'Notification', 'ActivityLog', 'ResourceWatcher', 'CustomRole', 'PermissionRule', 'ShareLink', 'Subscription', 'Entitlement', 'Invoice', 'PaymentMethod', 'External')));
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_agent_time ON automation.ai_agent_runs(workspace_id, ai_agent_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_status ON automation.ai_agent_runs(status, created_at) WHERE status IN ('Queued','Running');

-- -----------------------------------------------------------------------------
-- V4.13 Composite uniqueness and workspace consistency FKs
-- -----------------------------------------------------------------------------
















-- -----------------------------------------------------------------------------
-- V4.14 Full RLS coverage with WITH CHECK (clean full schema)
-- -----------------------------------------------------------------------------

ALTER TABLE automation.ai_agent_runs ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.ai_agents ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.automation_executions ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.automation_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.automation_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.outbox_messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE automation.scheduled_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.billing_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.entitlements ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.feature_usage_ledger ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.invoices ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.payment_methods ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.subscriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.usage_metric_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.usage_metrics ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.workspace_feature_usage ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.activity_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.attachments ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.mentions ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.notification_deliveries ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.notification_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.presence_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.reactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.resource_watchers ENABLE ROW LEVEL SECURITY;
ALTER TABLE collab.unread_counters ENABLE ROW LEVEL SECURITY;
ALTER TABLE docs.blocks ENABLE ROW LEVEL SECURITY;
ALTER TABLE docs.document_versions ENABLE ROW LEVEL SECURITY;
ALTER TABLE docs.page_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE docs.pages ENABLE ROW LEVEL SECURITY;
ALTER TABLE docs.resource_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.audit_retention_policies ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.custom_role_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.custom_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.field_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.permission_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.permission_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.resource_permission_inheritance_cache ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.resource_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.security_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.share_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.workspace_member_role_assignments ENABLE ROW LEVEL SECURITY;
ALTER TABLE governance.workspace_policies ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.api_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.scim_directory_syncs ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.sso_providers ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.calendar_event_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.calendar_integrations ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.inbound_webhook_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.integration_connections ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.integration_scopes ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.integration_secret_versions ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.integration_sync_cursors ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.webhook_deliveries ENABLE ROW LEVEL SECURITY;
ALTER TABLE integration.webhook_subscriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE ops.export_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE ops.idempotency_keys ENABLE ROW LEVEL SECURITY;
ALTER TABLE ops.import_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE reporting.dashboard_sources ENABLE ROW LEVEL SECURITY;
ALTER TABLE reporting.dashboard_widgets ENABLE ROW LEVEL SECURITY;
ALTER TABLE reporting.dashboards ENABLE ROW LEVEL SECURITY;
ALTER TABLE reporting.reporting_snapshots ENABLE ROW LEVEL SECURITY;
ALTER TABLE search.search_documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE search.search_index_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.approval_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.approval_steps ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_fields ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_groups ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_item_connections ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_item_labels ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_item_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_item_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_item_values ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_relations ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_subscribers ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_view_pins ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_view_user_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.board_views ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.boards ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.checklist_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.checklists ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.field_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.form_questions ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.form_submissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.forms ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.formula_dependencies ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.item_dependencies ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.item_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.labels ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.mirror_value_snapshots ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.relation_field_configs ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.rollup_snapshots ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.saved_filters ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.time_tracking_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE work.workload_allocations ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.spaces ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.workspace_invitations ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.workspace_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace.workspaces ENABLE ROW LEVEL SECURITY;

CREATE POLICY p_workspace_isolation ON automation.ai_agent_runs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.ai_agents
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.automation_executions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.automation_rules
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.automation_templates
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.outbox_messages
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON automation.scheduled_jobs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.billing_events
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.entitlements
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.feature_usage_ledger
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.invoices
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.payment_methods
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.subscriptions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.usage_metric_history
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.usage_metrics
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON billing.workspace_feature_usage
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.activity_logs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.attachments
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.comments
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.mentions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.notification_deliveries
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.notification_preferences
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.notifications
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.presence_sessions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.reactions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.resource_watchers
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON collab.unread_counters
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON docs.blocks
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON docs.document_versions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON docs.page_templates
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON docs.pages
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON docs.resource_links
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.audit_logs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.audit_retention_policies
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.custom_role_permissions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.custom_roles
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.field_permissions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.permission_rules
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.permission_templates
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.resource_permission_inheritance_cache
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.resource_permissions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.security_events
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.share_links
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.workspace_member_role_assignments
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON governance.workspace_policies
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON identity.api_tokens
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON identity.scim_directory_syncs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON identity.sso_providers
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.calendar_event_links
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.calendar_integrations
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.inbound_webhook_events
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.integration_connections
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.integration_scopes
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.integration_secret_versions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.integration_sync_cursors
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.webhook_deliveries
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON integration.webhook_subscriptions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON ops.export_jobs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_idempotency_scope_isolation ON ops.idempotency_keys
    FOR ALL TO notrelix_app
    USING (
        (workspace_id IS NOT NULL AND workspace_id = ops.current_workspace_id())
        OR (workspace_id IS NULL AND user_id IS NOT NULL AND user_id = ops.current_user_id())
    )
    WITH CHECK (
        (workspace_id IS NOT NULL AND workspace_id = ops.current_workspace_id())
        OR (workspace_id IS NULL AND user_id IS NOT NULL AND user_id = ops.current_user_id())
    );
CREATE POLICY p_workspace_isolation ON ops.import_jobs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON reporting.dashboard_sources
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON reporting.dashboard_widgets
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON reporting.dashboards
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON reporting.reporting_snapshots
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON search.search_documents
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON search.search_index_jobs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.approval_requests
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.approval_steps
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_fields
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_groups
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_item_connections
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_item_labels
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_item_links
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_item_members
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_item_values
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_items
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_relations
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_subscribers
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_templates
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_view_pins
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_view_user_preferences
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.board_views
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.boards
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.checklist_items
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.checklists
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.field_options
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.form_questions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.form_submissions
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.forms
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.formula_dependencies
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.item_dependencies
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.item_templates
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.labels
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.mirror_value_snapshots
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.relation_field_configs
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.rollup_snapshots
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.saved_filters
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.time_tracking_entries
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON work.workload_allocations
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.spaces
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.team_members
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.teams
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.workspace_invitations
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.workspace_members
    FOR ALL TO notrelix_app
    USING (workspace_id = ops.current_workspace_id())
    WITH CHECK (workspace_id = ops.current_workspace_id());
CREATE POLICY p_workspace_isolation ON workspace.workspaces
    FOR ALL TO notrelix_app
    USING (id = ops.current_workspace_id())
    WITH CHECK (id = ops.current_workspace_id());

-- -----------------------------------------------------------------------------
-- V4.15 Re-apply version triggers after V4 table creation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    r record;
    trigger_name text;
BEGIN
    FOR r IN SELECT * FROM (VALUES
        ('automation','ai_agents'),
        ('automation','automation_rules'),
        ('automation','automation_templates'),
        ('automation','scheduled_jobs'),
        ('billing','entitlements'),
        ('billing','payment_methods'),
        ('billing','plans'),
        ('billing','subscriptions'),
        ('billing','usage_metrics'),
        ('billing','workspace_feature_usage'),
        ('collab','comments'),
        ('collab','notification_preferences'),
        ('collab','unread_counters'),
        ('docs','blocks'),
        ('docs','page_templates'),
        ('docs','pages'),
        ('governance','audit_retention_policies'),
        ('governance','custom_roles'),
        ('governance','field_permissions'),
        ('governance','permission_rules'),
        ('governance','permission_templates'),
        ('governance','resource_permissions'),
        ('governance','workspace_policies'),
        ('identity','api_tokens'),
        ('identity','oauth_accounts'),
        ('identity','scim_directory_syncs'),
        ('identity','sso_providers'),
        ('identity','user_mfa_methods'),
        ('identity','user_profiles'),
        ('identity','user_security_settings'),
        ('identity','user_sessions'),
        ('identity','users'),
        ('integration','calendar_integrations'),
        ('integration','integration_connections'),
        ('integration','integration_sync_cursors'),
        ('integration','webhook_subscriptions'),
        ('reporting','dashboard_sources'),
        ('reporting','dashboard_widgets'),
        ('reporting','dashboards'),
        ('work','approval_requests'),
        ('work','board_fields'),
        ('work','board_groups'),
        ('work','board_item_values'),
        ('work','board_items'),
        ('work','board_item_connections'),
        ('work','board_relations'),
        ('work','board_subscribers'),
        ('work','board_templates'),
        ('work','board_view_user_preferences'),
        ('work','board_views'),
        ('work','boards'),
        ('work','checklist_items'),
        ('work','checklists'),
        ('work','field_options'),
        ('work','form_questions'),
        ('work','form_submissions'),
        ('work','forms'),
        ('work','item_dependencies'),
        ('work','item_templates'),
        ('work','relation_field_configs'),
        ('work','saved_filters'),
        ('work','time_tracking_entries'),
        ('work','workload_allocations'),
        ('workspace','spaces'),
        ('workspace','teams'),
        ('workspace','workspace_invitations'),
        ('workspace','workspace_members'),
        ('workspace','workspaces')
    ) AS t(schema_name, table_name)
    LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = r.schema_name
              AND table_name = r.table_name
              AND column_name = 'version'
        ) THEN
            trigger_name := 'trg_' || r.schema_name || '_' || r.table_name || '_version';
            EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I;', trigger_name, r.schema_name, r.table_name);
            EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE ON %I.%I FOR EACH ROW EXECUTE FUNCTION ops.increment_version();',
                           trigger_name, r.schema_name, r.table_name);
        END IF;
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- V4.16 Grants for app role
-- -----------------------------------------------------------------------------
GRANT USAGE ON SCHEMA identity, workspace, governance, work, docs, collab, automation, integration, billing, reporting, search, ops TO notrelix_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA identity, workspace, governance, work, docs, collab, automation, integration, billing, reporting, search, ops TO notrelix_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA identity, workspace, governance, work, docs, collab, automation, integration, billing, reporting, search, ops TO notrelix_app;

-- -----------------------------------------------------------------------------
-- V4.17 Comments
-- -----------------------------------------------------------------------------
COMMENT ON TABLE governance.permission_rules IS 'V4 policy-engine table for account/workspace/board/field/doc/dashboard/automation rules. Deny should override allow in application evaluator.';
COMMENT ON TABLE work.board_relations IS 'V4 Connect Boards / Mirror / Rollup relation definition between boards.';
COMMENT ON TABLE work.forms IS 'V4 WorkForms-style intake form connected to a board; submissions can create board items.';
COMMENT ON TABLE billing.workspace_feature_usage IS 'V4 current quota snapshot used by application entitlement checks before creating boards, items, automations, integrations, storage, etc.';
COMMENT ON TABLE automation.ai_agents IS 'V4 optional AI agent/workflow configuration table for future AI work platform features.';
COMMENT ON COLUMN work.board_items.version IS 'Optimistic concurrency token. Application should require expectedVersion for collaborative mutations.';

COMMENT ON TABLE ops.processed_events IS 'Infrastructure idempotent consumer inbox. Prevents duplicate event handling via UNIQUE(event_id, consumer_name). Not a Domain aggregate.';
COMMENT ON COLUMN ops.job_locks.fencing_token IS 'Monotonic fencing token used to prevent stale workers from writing after an expired lock is reacquired by another worker.';
COMMENT ON TABLE governance.resource_permission_inheritance_cache IS 'Security-sensitive authorization projection/cache. It is not source of truth; invalidate/rebuild when permission graph changes and fail closed when cache_version is stale.';
COMMENT ON TABLE ops.idempotency_keys IS 'Infrastructure idempotency record keyed by (scope, idempotency_key). Handles client/network retries without duplicating writes.';
COMMENT ON TABLE ops.import_jobs IS 'User-facing import workflow status/progress record. Infrastructure persistence model, not Domain aggregate.';
COMMENT ON TABLE ops.export_jobs IS 'User-facing export workflow status/result record. Store large files in object/file storage; DB stores references only.';
COMMENT ON TABLE search.search_index_jobs IS 'Search indexing queue with retry, locking and availability metadata. Projection infrastructure, not Domain aggregate.';


-- =============================================================================
-- End of V4 hardening patch
-- =============================================================================


COMMENT ON TABLE collab.activity_logs IS 'Append-only activity trail. Do not soft-delete. Use is_visible/hidden_* only for UI hiding, not audit erasure.';
COMMENT ON TABLE governance.permission_rules IS 'V4.1 source-of-truth for authorization policy evaluation. Application PermissionEvaluator should prefer this model.';
COMMENT ON TABLE governance.resource_permissions IS 'DEPRECATED in V4.1. Legacy level-based ACL/projection. Migrate to governance.permission_rules before removal.';


-- =============================================================================
-- V4.1 verification helpers
-- =============================================================================
-- A. No legacy is_deleted columns on normalized tables:
-- SELECT table_schema, table_name, column_name
-- FROM information_schema.columns
-- WHERE column_name = 'is_deleted'
--   AND (table_schema, table_name) IN (
--       ('work','board_items'), ('work','board_fields'), ('docs','blocks')
--   );
--
-- B. No RLS-enabled table without policy:
-- SELECT n.nspname, c.relname
-- FROM pg_class c
-- JOIN pg_namespace n ON n.oid = c.relnamespace
-- WHERE c.relrowsecurity = true
--   AND NOT EXISTS (
--       SELECT 1 FROM pg_policies p
--       WHERE p.schemaname = n.nspname AND p.tablename = c.relname
--   );
--
-- C. Hot table PK defaults removed:
-- SELECT table_schema, table_name, column_default
-- FROM information_schema.columns
-- WHERE column_name = 'id'
--   AND column_default LIKE '%gen_random_uuid%'
--   AND (table_schema, table_name) IN (
--       ('work','board_items'), ('work','board_item_values'), ('work','board_fields'),
--       ('docs','blocks'), ('collab','activity_logs'), ('collab','comments'),
--       ('collab','notifications'), ('collab','attachments'), ('automation','outbox_messages'),
--       ('automation','automation_executions'), ('integration','webhook_deliveries'),
--       ('billing','feature_usage_ledger'), ('search','search_documents'), ('search','search_index_jobs')
--   );
-- =============================================================================


-- D. Enterprise operational support checks:
-- SELECT table_schema, table_name
-- FROM information_schema.tables
-- WHERE (table_schema, table_name) IN (
--   ('ops','idempotency_keys'), ('ops','processed_events'), ('ops','import_jobs'),
--   ('ops','export_jobs'), ('ops','job_locks'),
--   ('governance','resource_permission_inheritance_cache'),
--   ('search','search_index_jobs')
-- );
--
-- E. Required helper functions:
-- SELECT proname FROM pg_proc p JOIN pg_namespace n ON p.pronamespace = n.oid
-- WHERE n.nspname = 'ops' AND proname IN ('current_workspace_id','current_user_id','current_correlation_id');
--
-- F. Idempotency NULL-safe uniqueness is expression-free and stable:
-- SELECT indexname FROM pg_indexes
-- WHERE schemaname = 'ops' AND tablename = 'idempotency_keys'
--   AND indexname = 'ux_ops_idempotency_keys_scope_key';

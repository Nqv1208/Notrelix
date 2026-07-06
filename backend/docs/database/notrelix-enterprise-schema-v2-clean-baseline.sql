-- =============================================================================
-- Notrelix Enterprise Schema V2 — Clean SaaS Enterprise Baseline
-- =============================================================================
-- Generated: 2026-07-01
-- Source input: notrelix-enterprise-schema-v5-0-final(1).sql
-- Purpose: clean baseline for a modular-monolith Enterprise SaaS, service-ready later.
-- This file intentionally removes V5 compatibility/legacy tables and adds account-root tenancy.
-- =============================================================================

BEGIN;

-- SECTION 1: EXTENSIONS
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gin;

-- SECTION 2: ROLES
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_app') THEN
        CREATE ROLE notrelix_app NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_worker') THEN
        CREATE ROLE notrelix_worker NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_support_readonly') THEN
        CREATE ROLE notrelix_support_readonly NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_migration') THEN
        CREATE ROLE notrelix_migration NOLOGIN;
    END IF;
END $$;

-- SECTION 3: SCHEMAS
CREATE SCHEMA IF NOT EXISTS account;
COMMENT ON SCHEMA account IS 'Enterprise tenant/customer/account boundary; legal, billing, SSO, SCIM, data residency root.';
CREATE SCHEMA IF NOT EXISTS identity;
COMMENT ON SCHEMA identity IS 'Global user identity, authentication, sessions, MFA, OAuth and user-owned tokens.';
CREATE SCHEMA IF NOT EXISTS workspace;
COMMENT ON SCHEMA workspace IS 'Product workspace, spaces, teams, workspace membership and workspace invitations.';
CREATE SCHEMA IF NOT EXISTS governance;
COMMENT ON SCHEMA governance IS 'Authorization source model: custom roles, permissions, share links, policies and inheritance cache.';
CREATE SCHEMA IF NOT EXISTS authz;
COMMENT ON SCHEMA authz IS 'Local RLS authorization projection generated from account/workspace/governance source state.';
CREATE SCHEMA IF NOT EXISTS work;
COMMENT ON SCHEMA work IS 'Work-management source state: boards, groups, fields, items, views, forms, relations, approvals and workload.';
CREATE SCHEMA IF NOT EXISTS docs;
COMMENT ON SCHEMA docs IS 'Document/page/block source state.';
CREATE SCHEMA IF NOT EXISTS collab;
COMMENT ON SCHEMA collab IS 'Collaboration source state: comments, mentions, reactions, attachments, watchers, presence and read state.';
CREATE SCHEMA IF NOT EXISTS automation;
COMMENT ON SCHEMA automation IS 'Automation rules, executions, schedules, templates and AI agent execution state.';
CREATE SCHEMA IF NOT EXISTS integration;
COMMENT ON SCHEMA integration IS 'External integrations, OAuth/secret versions, outbound/inbound webhooks and calendar sync.';
CREATE SCHEMA IF NOT EXISTS billing;
COMMENT ON SCHEMA billing IS 'Account-level commercial model: customers, plans, prices, subscriptions, invoices, entitlements and usage.';
CREATE SCHEMA IF NOT EXISTS reporting;
COMMENT ON SCHEMA reporting IS 'Dashboard/report configuration and report snapshots.';
CREATE SCHEMA IF NOT EXISTS search;
COMMENT ON SCHEMA search IS 'Rebuildable search projection and indexing jobs.';
CREATE SCHEMA IF NOT EXISTS notifications;
COMMENT ON SCHEMA notifications IS 'Canonical notification center, recipient state, channel delivery and email outbox.';
CREATE SCHEMA IF NOT EXISTS activity;
COMMENT ON SCHEMA activity IS 'Canonical user-facing activity feed projection and read state.';
CREATE SCHEMA IF NOT EXISTS analytics;
COMMENT ON SCHEMA analytics IS 'Analytical daily projections; not billing source of truth.';
CREATE SCHEMA IF NOT EXISTS events;
COMMENT ON SCHEMA events IS 'Append-only durable business event log; not broker/outbox.';
CREATE SCHEMA IF NOT EXISTS messaging;
COMMENT ON SCHEMA messaging IS 'Canonical IntegrationEvent outbox, delivery attempts and inbox/processed-event idempotency.';
CREATE SCHEMA IF NOT EXISTS audit;
COMMENT ON SCHEMA audit IS 'Compliance/security audit only; append-only operational history.';
CREATE SCHEMA IF NOT EXISTS ops;
COMMENT ON SCHEMA ops IS 'Runtime mechanics: API idempotency, locks, import/export and cleanup runs.';

GRANT USAGE ON SCHEMA account TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA identity TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA workspace TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA governance TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA authz TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA work TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA docs TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA collab TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA automation TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA integration TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA billing TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA reporting TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA search TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA notifications TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA activity TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA analytics TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA events TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA messaging TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA audit TO notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT USAGE ON SCHEMA ops TO notrelix_app, notrelix_worker, notrelix_support_readonly;

-- SECTION 4: BASE FUNCTIONS
CREATE OR REPLACE FUNCTION ops.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION authz.current_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT NULLIF(current_setting('app.current_user_id', true), '')::uuid;
$$;

CREATE OR REPLACE FUNCTION authz.current_account_id()
RETURNS uuid
LANGUAGE sql
STABLE
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT NULLIF(current_setting('app.current_account_id', true), '')::uuid;
$$;

CREATE OR REPLACE FUNCTION authz.current_workspace_id()
RETURNS uuid
LANGUAGE sql
STABLE
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT NULLIF(current_setting('app.current_workspace_id', true), '')::uuid;
$$;

CREATE OR REPLACE FUNCTION authz.current_request_scope()
RETURNS text
LANGUAGE sql
STABLE
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT COALESCE(NULLIF(current_setting('app.request_scope', true), ''), 'app');
$$;

CREATE OR REPLACE FUNCTION authz.is_worker_scope()
RETURNS boolean
LANGUAGE sql
STABLE
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT authz.current_request_scope() = 'worker';
$$;

-- SECTION 5: TABLES

-- ACCOUNT TABLES
CREATE TABLE IF NOT EXISTS account.accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug citext NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    legal_name varchar(240),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Trialing','Suspended','Closed','Deleted')),
    default_region_code varchar(32),
    plan_code varchar(80),
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    CHECK (btrim(name) <> ''),
    CHECK (slug::text ~ '^[a-z0-9][a-z0-9-]{1,78}[a-z0-9]$')
);
COMMENT ON TABLE account.accounts IS 'Enterprise account/tenant root. Billing, SSO, SCIM and data residency attach here, not directly to workspace.';


-- IDENTITY TABLES
CREATE TABLE IF NOT EXISTS identity.users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email citext NOT NULL UNIQUE,
    normalized_email citext NOT NULL UNIQUE,
    display_name varchar(160) NOT NULL,
    avatar_url text,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Invited','Suspended','Deleted')),
    last_login_at timestamptz,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    CHECK (btrim(display_name) <> ''),
    CHECK (normalized_email::text = upper(email::text))
);
COMMENT ON TABLE identity.users IS 'Global user identity. Enterprise tenant membership lives in account/workspace schemas.';

CREATE TABLE IF NOT EXISTS identity.user_profiles (
    user_id uuid PRIMARY KEY REFERENCES identity.users(id) ON DELETE CASCADE,
    timezone varchar(80) NOT NULL DEFAULT 'UTC',
    locale varchar(20) NOT NULL DEFAULT 'en',
    theme varchar(40) NOT NULL DEFAULT 'system',
    preferences_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE identity.user_profiles IS 'User-owned profile/preferences; one row per user.';

CREATE TABLE IF NOT EXISTS identity.user_sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    refresh_token_hash varchar(255) NOT NULL UNIQUE,
    device_id varchar(120),
    ip_address inet,
    user_agent text,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Revoked','Expired')),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (expires_at > created_at)
);
COMMENT ON TABLE identity.user_sessions IS 'Refresh-session state. Revocation/expiry must be state-machine guarded in Domain.';

CREATE TABLE IF NOT EXISTS identity.oauth_accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    provider varchar(80) NOT NULL,
    provider_subject varchar(200) NOT NULL,
    email citext,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (provider, provider_subject),
    UNIQUE (user_id, provider)
);

CREATE TABLE IF NOT EXISTS identity.user_security_settings (
    user_id uuid PRIMARY KEY REFERENCES identity.users(id) ON DELETE CASCADE,
    mfa_enabled boolean NOT NULL DEFAULT false,
    password_changed_at timestamptz,
    password_change_required boolean NOT NULL DEFAULT false,
    failed_login_count integer NOT NULL DEFAULT 0 CHECK (failed_login_count >= 0),
    locked_until timestamptz,
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.user_mfa_methods (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    method_type varchar(32) NOT NULL CHECK (method_type IN ('Totp','WebAuthn','RecoveryCode','Sms','Email')),
    secret_ref varchar(200),
    display_name varchar(120),
    is_verified boolean NOT NULL DEFAULT false,
    is_primary boolean NOT NULL DEFAULT false,
    enabled boolean NOT NULL DEFAULT true,
    verified_at timestamptz,
    last_used_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (user_id, method_type, display_name)
);

CREATE TABLE IF NOT EXISTS identity.user_login_attempts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    email citext NOT NULL,
    success boolean NOT NULL,
    failure_reason varchar(120),
    ip_address inet,
    user_agent text,
    attempted_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.email_verification_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash varchar(255) NOT NULL UNIQUE,
    email citext NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.password_reset_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash varchar(255) NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.user_api_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    name varchar(120) NOT NULL,
    token_hash varchar(255) NOT NULL UNIQUE,
    scopes text[] NOT NULL DEFAULT '{}'::text[],
    last_used_at timestamptz,
    expires_at timestamptz,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE identity.user_api_tokens IS 'User API tokens may be account-scoped; global tokens use null account_id.';


-- ACCOUNT TABLES
CREATE TABLE IF NOT EXISTS account.account_members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role varchar(40) NOT NULL CHECK (role IN ('Owner','Admin','Member','BillingAdmin','SecurityAdmin','Guest')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Invited','Suspended','Removed')),
    joined_at timestamptz,
    invited_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, user_id)
);
COMMENT ON TABLE account.account_members IS 'Enterprise account membership. Workspace membership must be scoped under this root.';

CREATE TABLE IF NOT EXISTS account.account_invitations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    email citext NOT NULL,
    role varchar(40) NOT NULL CHECK (role IN ('Admin','Member','BillingAdmin','SecurityAdmin','Guest')),
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Accepted','Revoked','Expired')),
    token_hash varchar(255) NOT NULL UNIQUE,
    invited_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    expires_at timestamptz NOT NULL,
    accepted_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (expires_at > created_at)
);
COMMENT ON TABLE account.account_invitations IS 'Enterprise account invitation, separate from workspace invitation.';

CREATE TABLE IF NOT EXISTS account.account_domains (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    domain citext NOT NULL,
    verification_status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (verification_status IN ('Pending','Verified','Rejected')),
    verification_token_hash varchar(255),
    verified_at timestamptz,
    auto_join_enabled boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (domain)
);

CREATE TABLE IF NOT EXISTS account.account_settings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    setting_key varchar(120) NOT NULL,
    setting_value jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, setting_key)
);

CREATE TABLE IF NOT EXISTS account.account_regions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    region_code varchar(32) NOT NULL,
    data_residency_mode varchar(32) NOT NULL DEFAULT 'Default' CHECK (data_residency_mode IN ('Default','Pinned','Migrating')),
    is_primary boolean NOT NULL DEFAULT false,
    migration_status varchar(32),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, region_code)
);

CREATE TABLE IF NOT EXISTS account.account_identity_providers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    provider_type varchar(32) NOT NULL CHECK (provider_type IN ('Saml','Oidc')),
    name varchar(120) NOT NULL,
    issuer varchar(300) NOT NULL,
    sso_url text NOT NULL,
    certificate_ref varchar(255),
    status varchar(32) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Active','Disabled')),
    jit_provisioning_enabled boolean NOT NULL DEFAULT false,
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, provider_type, issuer)
);
COMMENT ON TABLE account.account_identity_providers IS 'Enterprise SSO provider. Moved out of identity because this is account-level tenant config.';

CREATE TABLE IF NOT EXISTS account.scim_directories (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    identity_provider_id uuid REFERENCES account.account_identity_providers(id) ON DELETE SET NULL,
    name varchar(120) NOT NULL,
    base_url text,
    bearer_token_hash varchar(255),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Error')),
    last_sync_at timestamptz,
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, name)
);

CREATE TABLE IF NOT EXISTS account.scim_sync_runs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    directory_id uuid NOT NULL REFERENCES account.scim_directories(id) ON DELETE CASCADE,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled')),
    started_at timestamptz,
    finished_at timestamptz,
    users_created integer NOT NULL DEFAULT 0 CHECK (users_created >= 0),
    users_updated integer NOT NULL DEFAULT 0 CHECK (users_updated >= 0),
    users_disabled integer NOT NULL DEFAULT 0 CHECK (users_disabled >= 0),
    error_message text,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS account.workspace_routes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid,
    route_slug citext NOT NULL,
    is_default boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, route_slug)
);
COMMENT ON TABLE account.workspace_routes IS 'Stable account-scoped route registry for workspace URLs. FK to workspace added after workspace table exists by application migration if needed.';


-- WORKSPACE TABLES
CREATE TABLE IF NOT EXISTS workspace.workspaces (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    slug citext NOT NULL,
    name varchar(160) NOT NULL,
    description varchar(5000),
    visibility varchar(32) NOT NULL DEFAULT 'Private' CHECK (visibility IN ('Private','Account','Public')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Suspended','Deleted')),
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, slug),
    UNIQUE (account_id, id),
    CHECK (slug::text ~ '^[a-z0-9][a-z0-9-]{1,78}[a-z0-9]$'),
    CHECK (btrim(name) <> '')
);

CREATE TABLE IF NOT EXISTS workspace.workspace_members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role varchar(40) NOT NULL CHECK (role IN ('Owner','Admin','Member','Guest','Viewer')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Invited','Suspended','Removed')),
    joined_at timestamptz,
    last_seen_at timestamptz,
    invited_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, user_id),
    FOREIGN KEY (account_id, workspace_id) REFERENCES workspace.workspaces(account_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS workspace.workspace_invitations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    email citext NOT NULL,
    role varchar(40) NOT NULL CHECK (role IN ('Admin','Member','Guest','Viewer')),
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Accepted','Revoked','Expired')),
    token_hash varchar(255) NOT NULL UNIQUE,
    invited_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    expires_at timestamptz NOT NULL,
    accepted_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (account_id, workspace_id) REFERENCES workspace.workspaces(account_id, id) ON DELETE CASCADE,
    CHECK (expires_at > created_at)
);

CREATE TABLE IF NOT EXISTS workspace.spaces (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    parent_space_id uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    slug citext NOT NULL,
    name varchar(160) NOT NULL,
    description varchar(5000),
    position numeric(12,4) NOT NULL DEFAULT 0,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, slug),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id) REFERENCES workspace.workspaces(account_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS workspace.teams (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    slug citext NOT NULL,
    name varchar(160) NOT NULL,
    description varchar(5000),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, slug),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id) REFERENCES workspace.workspaces(account_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS workspace.team_members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    team_id uuid NOT NULL REFERENCES workspace.teams(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role varchar(40) NOT NULL DEFAULT 'Member' CHECK (role IN ('Owner','Manager','Member')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Suspended','Removed')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (team_id, user_id),
    FOREIGN KEY (account_id, workspace_id, team_id) REFERENCES workspace.teams(account_id, workspace_id, id) ON DELETE CASCADE
);


-- GOVERNANCE TABLES
CREATE TABLE IF NOT EXISTS governance.custom_roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    code varchar(80) NOT NULL,
    name varchar(100) NOT NULL,
    description varchar(500),
    scope varchar(32) NOT NULL CHECK (scope IN ('Account','Workspace')),
    is_system boolean NOT NULL DEFAULT false,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, workspace_id, code)
);

CREATE TABLE IF NOT EXISTS governance.custom_role_permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    custom_role_id uuid NOT NULL REFERENCES governance.custom_roles(id) ON DELETE CASCADE,
    permission_code varchar(120) NOT NULL,
    effect varchar(16) NOT NULL DEFAULT 'Allow' CHECK (effect IN ('Allow','Deny')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (custom_role_id, permission_code)
);

CREATE TABLE IF NOT EXISTS governance.workspace_member_role_assignments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    workspace_member_id uuid NOT NULL REFERENCES workspace.workspace_members(id) ON DELETE CASCADE,
    custom_role_id uuid NOT NULL REFERENCES governance.custom_roles(id) ON DELETE CASCADE,
    assigned_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_member_id, custom_role_id)
);

CREATE TABLE IF NOT EXISTS governance.resource_permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    subject_type varchar(40) NOT NULL CHECK (subject_type IN ('User','Team','Role','Account')),
    subject_id uuid NOT NULL,
    permission_level varchar(40) NOT NULL CHECK (permission_level IN ('Owner','Manager','Editor','Commenter','Viewer','None')),
    granted_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    expires_at timestamptz,
    revoked_at timestamptz,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, workspace_id, resource_type, resource_id, subject_type, subject_id)
);

CREATE TABLE IF NOT EXISTS governance.field_permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id uuid NOT NULL,
    field_id uuid NOT NULL,
    subject_type varchar(40) NOT NULL CHECK (subject_type IN ('User','Team','Role')),
    subject_id uuid NOT NULL,
    can_view boolean NOT NULL DEFAULT true,
    can_edit boolean NOT NULL DEFAULT false,
    can_mask boolean NOT NULL DEFAULT false,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (field_id, subject_type, subject_id),
    CHECK (can_edit = false OR can_view = true),
    CHECK (can_mask = false OR can_view = true)
);

CREATE TABLE IF NOT EXISTS governance.permission_rules (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    resource_type varchar(80) NOT NULL,
    condition_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    effect_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    priority integer NOT NULL DEFAULT 0,
    enabled boolean NOT NULL DEFAULT true,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS governance.permission_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    resource_type varchar(80) NOT NULL,
    template_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_system boolean NOT NULL DEFAULT false,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, workspace_id, name, resource_type)
);

CREATE TABLE IF NOT EXISTS governance.workspace_policies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    policy_key varchar(120) NOT NULL,
    policy_value jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, policy_key)
);

CREATE TABLE IF NOT EXISTS governance.share_links (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    token_hash varchar(255) NOT NULL UNIQUE,
    permission_level varchar(40) NOT NULL CHECK (permission_level IN ('Viewer','Commenter','Editor')),
    enabled boolean NOT NULL DEFAULT true,
    expires_at timestamptz,
    created_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS governance.resource_permission_inheritance_cache (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    subject_type varchar(40) NOT NULL,
    subject_id uuid NOT NULL,
    effective_permission_level varchar(40) NOT NULL,
    source_permission_id uuid REFERENCES governance.resource_permissions(id) ON DELETE SET NULL,
    source_version integer NOT NULL DEFAULT 1,
    computed_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, resource_type, resource_id, subject_type, subject_id)
);


-- AUTHZ TABLES
CREATE TABLE IF NOT EXISTS authz.access_grants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role_codes text[] NOT NULL DEFAULT '{}'::text[],
    permission_codes text[] NOT NULL DEFAULT '{}'::text[],
    is_account_admin boolean NOT NULL DEFAULT false,
    is_workspace_admin boolean NOT NULL DEFAULT false,
    source_event_id uuid,
    source_version bigint NOT NULL DEFAULT 1,
    expires_at timestamptz,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, user_id)
);
COMMENT ON TABLE authz.access_grants IS 'RLS read model. Source of truth remains account/workspace/governance.';


-- WORK TABLES
CREATE TABLE IF NOT EXISTS work.boards (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    space_id uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    title varchar(255) NOT NULL,
    description varchar(5000),
    visibility varchar(32) NOT NULL DEFAULT 'Private' CHECK (visibility IN ('Private','Workspace','Public')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Deleted')),
    item_key_prefix varchar(10) NOT NULL,
    next_item_sequence bigint NOT NULL DEFAULT 1 CHECK (next_item_sequence >= 1),
    default_group_id uuid,
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, item_key_prefix),
    UNIQUE (account_id, workspace_id, id),
    CHECK (btrim(title) <> ''),
    CHECK (item_key_prefix ~ '^[A-Z][A-Z0-9]{0,9}$')
);

CREATE TABLE IF NOT EXISTS work.board_groups (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    title varchar(255) NOT NULL,
    position numeric(12,4) NOT NULL DEFAULT 0,
    color varchar(40),
    is_default boolean NOT NULL DEFAULT false,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (board_id, title),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_fields (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    name varchar(100) NOT NULL,
    field_type varchar(40) NOT NULL CHECK (field_type IN ('Text','LongText','Number','Date','Status','Select','MultiSelect','People','Checkbox','Url','Email','Phone','Formula','Relation','Mirror','Rollup','Files','Timeline')),
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_required boolean NOT NULL DEFAULT false,
    is_formula boolean NOT NULL DEFAULT false,
    formula_expression text,
    position numeric(12,4) NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (board_id, name),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.field_options (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    field_id uuid NOT NULL,
    label varchar(120) NOT NULL,
    color varchar(40),
    position numeric(12,4) NOT NULL DEFAULT 0,
    is_archived boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (field_id, label),
    FOREIGN KEY (account_id, workspace_id, field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    group_id uuid REFERENCES work.board_groups(id) ON DELETE SET NULL,
    parent_item_id uuid REFERENCES work.board_items(id) ON DELETE SET NULL,
    item_key varchar(40) NOT NULL,
    name varchar(500) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open','Completed','Archived','Deleted')),
    position numeric(12,4) NOT NULL DEFAULT 0,
    completed_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (board_id, item_key),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_item_values (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    item_id uuid NOT NULL,
    field_id uuid NOT NULL,
    value_json jsonb NOT NULL DEFAULT 'null'::jsonb,
    value_text text,
    value_number numeric,
    value_date timestamptz,
    value_bool boolean,
    value_user_ids uuid[],
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (item_id, field_id),
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_item_members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    assigned_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (item_id, user_id),
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.labels (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(100) NOT NULL,
    color varchar(40),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, name),
    UNIQUE (account_id, workspace_id, id)
);

CREATE TABLE IF NOT EXISTS work.board_item_labels (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    label_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (item_id, label_id),
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, label_id) REFERENCES work.labels(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_views (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    name varchar(255) NOT NULL,
    view_type varchar(40) NOT NULL CHECK (view_type IN ('Table','Kanban','Calendar','Timeline','Gantt','Form','Chart','Dashboard')),
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_default boolean NOT NULL DEFAULT false,
    position numeric(12,4) NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (board_id, name),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_view_user_preferences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    view_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    preferences_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (view_id, user_id),
    FOREIGN KEY (account_id, workspace_id, view_id) REFERENCES work.board_views(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.saved_filters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    name varchar(160) NOT NULL,
    filter_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_view_pins (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    view_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (view_id, user_id),
    FOREIGN KEY (account_id, workspace_id, view_id) REFERENCES work.board_views(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_item_links (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    url text NOT NULL,
    title varchar(255),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.checklists (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    title varchar(255) NOT NULL,
    position numeric(12,4) NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.checklist_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    checklist_id uuid NOT NULL REFERENCES work.checklists(id) ON DELETE CASCADE,
    content varchar(500) NOT NULL,
    is_completed boolean NOT NULL DEFAULT false,
    completed_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    completed_at timestamptz,
    position numeric(12,4) NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS work.relation_field_configs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    field_id uuid NOT NULL,
    target_board_id uuid NOT NULL,
    allow_multiple boolean NOT NULL DEFAULT true,
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (field_id),
    FOREIGN KEY (account_id, workspace_id, field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, target_board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_relations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    source_board_id uuid NOT NULL,
    target_board_id uuid NOT NULL,
    name varchar(160) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Paused','Archived','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, source_board_id, target_board_id, name),
    FOREIGN KEY (account_id, workspace_id, source_board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, target_board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.board_item_connections (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    relation_id uuid NOT NULL REFERENCES work.board_relations(id) ON DELETE CASCADE,
    source_item_id uuid NOT NULL,
    target_item_id uuid NOT NULL,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (relation_id, source_item_id, target_item_id),
    FOREIGN KEY (account_id, workspace_id, source_item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, target_item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.formula_dependencies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    formula_field_id uuid NOT NULL,
    depends_on_field_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (formula_field_id, depends_on_field_id),
    FOREIGN KEY (account_id, workspace_id, formula_field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, depends_on_field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE,
    CHECK (formula_field_id <> depends_on_field_id)
);

CREATE TABLE IF NOT EXISTS work.mirror_value_snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    field_id uuid NOT NULL,
    source_item_id uuid NOT NULL,
    source_field_id uuid NOT NULL,
    value_json jsonb NOT NULL DEFAULT 'null'::jsonb,
    computed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (item_id, field_id),
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.rollup_snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    field_id uuid NOT NULL,
    value_json jsonb NOT NULL DEFAULT 'null'::jsonb,
    computed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (item_id, field_id),
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, field_id) REFERENCES work.board_fields(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.approval_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    requested_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Approved','Rejected','Cancelled')),
    reason text,
    decided_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.approval_steps (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    approval_request_id uuid NOT NULL REFERENCES work.approval_requests(id) ON DELETE CASCADE,
    approver_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    step_order integer NOT NULL CHECK (step_order > 0),
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Approved','Rejected','Skipped')),
    comment text,
    decided_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (approval_request_id, step_order)
);

CREATE TABLE IF NOT EXISTS work.workload_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    item_id uuid REFERENCES work.board_items(id) ON DELETE CASCADE,
    allocation_date date NOT NULL,
    allocation_minutes integer NOT NULL DEFAULT 0 CHECK (allocation_minutes >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, user_id, item_id, allocation_date)
);

CREATE TABLE IF NOT EXISTS work.board_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    template_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_system boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS work.item_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    board_id uuid NOT NULL REFERENCES work.boards(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    template_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (board_id, name)
);

CREATE TABLE IF NOT EXISTS work.board_subscribers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (board_id, user_id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.item_dependencies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    blocking_item_id uuid NOT NULL,
    blocked_item_id uuid NOT NULL,
    dependency_type varchar(40) NOT NULL DEFAULT 'Blocks' CHECK (dependency_type IN ('Blocks','RelatesTo')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (blocking_item_id, blocked_item_id),
    CHECK (blocking_item_id <> blocked_item_id),
    FOREIGN KEY (account_id, workspace_id, blocking_item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    FOREIGN KEY (account_id, workspace_id, blocked_item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.time_tracking_entries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    item_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    started_at timestamptz NOT NULL,
    ended_at timestamptz,
    duration_minutes integer CHECK (duration_minutes IS NULL OR duration_minutes >= 0),
    description varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, item_id) REFERENCES work.board_items(account_id, workspace_id, id) ON DELETE CASCADE,
    CHECK (ended_at IS NULL OR ended_at >= started_at)
);

CREATE TABLE IF NOT EXISTS work.forms (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    board_id uuid NOT NULL,
    slug citext NOT NULL,
    title varchar(255) NOT NULL,
    description varchar(5000),
    status varchar(32) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Published','Paused','Archived','Deleted')),
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, slug),
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, board_id) REFERENCES work.boards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.form_questions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    form_id uuid NOT NULL,
    field_id uuid REFERENCES work.board_fields(id) ON DELETE SET NULL,
    label varchar(255) NOT NULL,
    question_type varchar(40) NOT NULL,
    is_required boolean NOT NULL DEFAULT false,
    position numeric(12,4) NOT NULL DEFAULT 0,
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, form_id) REFERENCES work.forms(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS work.form_submissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    form_id uuid NOT NULL,
    created_item_id uuid REFERENCES work.board_items(id) ON DELETE SET NULL,
    submitted_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    submitter_email citext,
    answers_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    ip_address inet,
    user_agent text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (account_id, workspace_id, form_id) REFERENCES work.forms(account_id, workspace_id, id) ON DELETE CASCADE
);


-- DOCS TABLES
CREATE TABLE IF NOT EXISTS docs.pages (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    parent_page_id uuid REFERENCES docs.pages(id) ON DELETE SET NULL,
    space_id uuid REFERENCES workspace.spaces(id) ON DELETE SET NULL,
    slug citext NOT NULL,
    title varchar(500) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived','Deleted')),
    icon varchar(80),
    cover_url text,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, slug),
    UNIQUE (account_id, workspace_id, id)
);

CREATE TABLE IF NOT EXISTS docs.blocks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    page_id uuid NOT NULL,
    parent_block_id uuid REFERENCES docs.blocks(id) ON DELETE CASCADE,
    block_type varchar(40) NOT NULL,
    content_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    position numeric(12,4) NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, workspace_id, id),
    FOREIGN KEY (account_id, workspace_id, page_id) REFERENCES docs.pages(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS docs.document_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    page_id uuid NOT NULL,
    version_number integer NOT NULL CHECK (version_number > 0),
    snapshot_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (page_id, version_number),
    FOREIGN KEY (account_id, workspace_id, page_id) REFERENCES docs.pages(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS docs.resource_links (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    source_resource_type varchar(80) NOT NULL,
    source_resource_id uuid NOT NULL,
    target_resource_type varchar(80) NOT NULL,
    target_resource_id uuid NOT NULL,
    link_type varchar(40) NOT NULL DEFAULT 'Related',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, source_resource_type, source_resource_id, target_resource_type, target_resource_id, link_type)
);

CREATE TABLE IF NOT EXISTS docs.page_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    template_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_system boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);


-- COLLAB TABLES
CREATE TABLE IF NOT EXISTS collab.comments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    target_resource_type varchar(80) NOT NULL,
    target_resource_id uuid NOT NULL,
    parent_comment_id uuid REFERENCES collab.comments(id) ON DELETE CASCADE,
    author_user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    content text NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Resolved','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    CHECK (char_length(content) <= 10000),
    UNIQUE (account_id, workspace_id, id)
);

CREATE TABLE IF NOT EXISTS collab.reactions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    target_resource_type varchar(80) NOT NULL,
    target_resource_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    emoji varchar(40) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, target_resource_type, target_resource_id, user_id, emoji)
);

CREATE TABLE IF NOT EXISTS collab.mentions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    source_resource_type varchar(80) NOT NULL,
    source_resource_id uuid NOT NULL,
    mentioned_user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    mentioned_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    read_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS collab.attachments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    file_name varchar(255) NOT NULL,
    mime_type varchar(120) NOT NULL,
    size_bytes bigint NOT NULL CHECK (size_bytes >= 0),
    storage_key varchar(500) NOT NULL,
    uploaded_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS collab.resource_watchers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, resource_type, resource_id, user_id)
);

CREATE TABLE IF NOT EXISTS collab.presence_sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    resource_type varchar(80),
    resource_id uuid,
    connection_id varchar(160) NOT NULL UNIQUE,
    status varchar(32) NOT NULL DEFAULT 'Online' CHECK (status IN ('Online','Idle','Offline')),
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS collab.resource_read_states (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    last_read_at timestamptz NOT NULL DEFAULT now(),
    last_seen_version bigint,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, resource_type, resource_id, user_id)
);


-- AUTOMATION TABLES
CREATE TABLE IF NOT EXISTS automation.automation_rules (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    trigger_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    conditions_json jsonb NOT NULL DEFAULT '[]'::jsonb,
    actions_json jsonb NOT NULL DEFAULT '[]'::jsonb,
    status varchar(32) NOT NULL DEFAULT 'Disabled' CHECK (status IN ('Enabled','Disabled','Archived','Deleted')),
    last_run_at timestamptz,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS automation.automation_executions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    automation_rule_id uuid NOT NULL REFERENCES automation.automation_rules(id) ON DELETE CASCADE,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled')),
    trigger_payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    result_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    started_at timestamptz,
    finished_at timestamptz,
    error_message text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS automation.scheduled_jobs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type varchar(120) NOT NULL,
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled')),
    run_at timestamptz NOT NULL,
    locked_by varchar(120),
    locked_until timestamptz,
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    last_error text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS automation.automation_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    template_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_system boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS automation.ai_agents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    status varchar(32) NOT NULL DEFAULT 'Disabled' CHECK (status IN ('Enabled','Disabled','Error','Archived','Deleted')),
    model_policy_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    tool_policy_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, name)
);

CREATE TABLE IF NOT EXISTS automation.ai_agent_runs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    agent_id uuid NOT NULL REFERENCES automation.ai_agents(id) ON DELETE CASCADE,
    initiated_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled','RequiresApproval')),
    input_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    output_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    started_at timestamptz,
    finished_at timestamptz,
    error_message text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);


-- INTEGRATION TABLES
CREATE TABLE IF NOT EXISTS integration.integration_connections (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider varchar(80) NOT NULL,
    external_account_id varchar(200),
    display_name varchar(160) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Error','Revoked','Deleted')),
    connected_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, workspace_id, provider, external_account_id)
);

CREATE TABLE IF NOT EXISTS integration.integration_scopes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    scope varchar(160) NOT NULL,
    granted_at timestamptz NOT NULL DEFAULT now(),
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (connection_id, scope)
);

CREATE TABLE IF NOT EXISTS integration.integration_secret_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    version_number integer NOT NULL CHECK (version_number > 0),
    secret_ref varchar(255) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Revoked','Expired')),
    rotated_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (connection_id, version_number)
);

CREATE TABLE IF NOT EXISTS integration.webhook_subscriptions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    target_url text NOT NULL,
    event_names text[] NOT NULL DEFAULT '{}'::text[],
    secret_ref varchar(255) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Deleted')),
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    CHECK (target_url ~ '^https://')
);

CREATE TABLE IF NOT EXISTS integration.webhook_deliveries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    subscription_id uuid NOT NULL REFERENCES integration.webhook_subscriptions(id) ON DELETE CASCADE,
    event_id uuid NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Sending','Succeeded','Failed','DeadLettered')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz,
    last_status_code integer,
    last_error text,
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS integration.inbound_webhook_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    provider varchar(80) NOT NULL,
    external_event_id varchar(200) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Received' CHECK (status IN ('Received','Processing','Processed','Ignored','Failed')),
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    received_at timestamptz NOT NULL DEFAULT now(),
    processed_at timestamptz,
    error_message text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (provider, external_event_id)
);

CREATE TABLE IF NOT EXISTS integration.calendar_integrations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    calendar_id varchar(200) NOT NULL,
    sync_direction varchar(32) NOT NULL DEFAULT 'TwoWay' CHECK (sync_direction IN ('ImportOnly','ExportOnly','TwoWay')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Error')),
    settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (connection_id, calendar_id)
);

CREATE TABLE IF NOT EXISTS integration.calendar_event_links (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    calendar_integration_id uuid NOT NULL REFERENCES integration.calendar_integrations(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    external_event_id varchar(200) NOT NULL,
    last_synced_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (calendar_integration_id, external_event_id),
    UNIQUE (workspace_id, resource_type, resource_id, calendar_integration_id)
);

CREATE TABLE IF NOT EXISTS integration.integration_sync_cursors (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    connection_id uuid NOT NULL REFERENCES integration.integration_connections(id) ON DELETE CASCADE,
    cursor_key varchar(160) NOT NULL,
    cursor_value text,
    last_synced_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (connection_id, cursor_key)
);


-- BILLING TABLES
CREATE TABLE IF NOT EXISTS billing.billing_customers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL UNIQUE REFERENCES account.accounts(id) ON DELETE CASCADE,
    provider varchar(80) NOT NULL,
    provider_customer_id varchar(200) NOT NULL,
    billing_email citext,
    tax_info_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Delinquent','Suspended','Deleted')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (provider, provider_customer_id)
);

CREATE TABLE IF NOT EXISTS billing.plans (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived')),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS billing.plan_prices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id uuid NOT NULL REFERENCES billing.plans(id) ON DELETE CASCADE,
    provider varchar(80),
    provider_price_id varchar(200),
    currency char(3) NOT NULL DEFAULT 'USD',
    billing_interval varchar(20) NOT NULL CHECK (billing_interval IN ('Monthly','Yearly','OneTime')),
    unit_amount_cents bigint NOT NULL CHECK (unit_amount_cents >= 0),
    billing_scheme varchar(32) NOT NULL DEFAULT 'PerSeat' CHECK (billing_scheme IN ('Flat','PerSeat','UsageBased')),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Archived')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (plan_id, currency, billing_interval, billing_scheme),
    UNIQUE (provider, provider_price_id)
);

CREATE TABLE IF NOT EXISTS billing.plan_limits (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id uuid NOT NULL REFERENCES billing.plans(id) ON DELETE CASCADE,
    feature_code varchar(120) NOT NULL,
    limit_value bigint,
    limit_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (plan_id, feature_code)
);

CREATE TABLE IF NOT EXISTS billing.subscriptions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    billing_customer_id uuid REFERENCES billing.billing_customers(id) ON DELETE SET NULL,
    plan_id uuid NOT NULL REFERENCES billing.plans(id),
    provider varchar(80),
    provider_subscription_id varchar(200),
    status varchar(32) NOT NULL DEFAULT 'Trialing' CHECK (status IN ('Trialing','Active','PastDue','Paused','Canceled','Expired')),
    current_period_start timestamptz,
    current_period_end timestamptz,
    cancel_at_period_end boolean NOT NULL DEFAULT false,
    canceled_at timestamptz,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (provider, provider_subscription_id)
);

CREATE TABLE IF NOT EXISTS billing.subscription_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    subscription_id uuid NOT NULL REFERENCES billing.subscriptions(id) ON DELETE CASCADE,
    plan_price_id uuid NOT NULL REFERENCES billing.plan_prices(id),
    provider_subscription_item_id varchar(200),
    quantity integer NOT NULL DEFAULT 1 CHECK (quantity > 0),
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Canceled')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (subscription_id, plan_price_id)
);

CREATE TABLE IF NOT EXISTS billing.payment_methods (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    billing_customer_id uuid REFERENCES billing.billing_customers(id) ON DELETE SET NULL,
    provider varchar(80) NOT NULL,
    provider_payment_method_id varchar(200) NOT NULL,
    method_type varchar(40) NOT NULL,
    brand varchar(60),
    last4 varchar(8),
    exp_month integer CHECK (exp_month BETWEEN 1 AND 12),
    exp_year integer CHECK (exp_year >= 2000),
    is_default boolean NOT NULL DEFAULT false,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Expired','Removed')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (provider, provider_payment_method_id)
);

CREATE TABLE IF NOT EXISTS billing.invoices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    subscription_id uuid REFERENCES billing.subscriptions(id) ON DELETE SET NULL,
    provider varchar(80),
    provider_invoice_id varchar(200),
    invoice_number varchar(120),
    status varchar(32) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft','Open','Paid','Void','Uncollectible')),
    currency char(3) NOT NULL DEFAULT 'USD',
    subtotal_cents bigint NOT NULL DEFAULT 0 CHECK (subtotal_cents >= 0),
    tax_cents bigint NOT NULL DEFAULT 0 CHECK (tax_cents >= 0),
    total_cents bigint NOT NULL DEFAULT 0 CHECK (total_cents >= 0),
    due_at timestamptz,
    paid_at timestamptz,
    invoice_pdf_url text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (provider, provider_invoice_id)
);

CREATE TABLE IF NOT EXISTS billing.invoice_line_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    invoice_id uuid NOT NULL REFERENCES billing.invoices(id) ON DELETE CASCADE,
    description varchar(500) NOT NULL,
    quantity numeric(12,2) NOT NULL DEFAULT 1 CHECK (quantity >= 0),
    unit_amount_cents bigint NOT NULL DEFAULT 0 CHECK (unit_amount_cents >= 0),
    amount_cents bigint NOT NULL DEFAULT 0 CHECK (amount_cents >= 0),
    period_start timestamptz,
    period_end timestamptz,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS billing.entitlements (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    target_scope varchar(32) NOT NULL CHECK (target_scope IN ('Account','Workspace')),
    target_workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code varchar(120) NOT NULL,
    limit_value bigint,
    status varchar(32) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Disabled','Revoked','Expired')),
    source varchar(40) NOT NULL DEFAULT 'Plan' CHECK (source IN ('Plan','Trial','Promotion','Manual','EnterpriseContract')),
    expires_at timestamptz,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (account_id, target_scope, target_workspace_id, feature_code),
    CHECK ((target_scope = 'Account' AND target_workspace_id IS NULL) OR (target_scope = 'Workspace' AND target_workspace_id IS NOT NULL))
);

CREATE TABLE IF NOT EXISTS billing.usage_metrics (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    target_scope varchar(32) NOT NULL CHECK (target_scope IN ('Account','Workspace')),
    target_workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    metric_key varchar(120) NOT NULL,
    current_value bigint NOT NULL DEFAULT 0 CHECK (current_value >= 0),
    limit_value bigint,
    period_start timestamptz NOT NULL,
    period_end timestamptz NOT NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, target_scope, target_workspace_id, metric_key, period_start),
    CHECK (period_end > period_start)
);

CREATE TABLE IF NOT EXISTS billing.usage_metric_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    usage_metric_id uuid REFERENCES billing.usage_metrics(id) ON DELETE SET NULL,
    metric_key varchar(120) NOT NULL,
    delta bigint NOT NULL,
    value_after bigint NOT NULL CHECK (value_after >= 0),
    reason varchar(120),
    recorded_at timestamptz NOT NULL DEFAULT now(),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS billing.feature_usage_ledger (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code varchar(120) NOT NULL,
    quantity bigint NOT NULL CHECK (quantity >= 0),
    occurred_at timestamptz NOT NULL DEFAULT now(),
    source_resource_type varchar(80),
    source_resource_id uuid,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS billing.billing_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    provider varchar(80) NOT NULL,
    external_event_id varchar(200) NOT NULL,
    event_type varchar(160) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Received' CHECK (status IN ('Received','Processed','Failed','Ignored')),
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    received_at timestamptz NOT NULL DEFAULT now(),
    processed_at timestamptz,
    error_message text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (provider, external_event_id)
);


-- REPORTING TABLES
CREATE TABLE IF NOT EXISTS reporting.dashboards (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    description varchar(5000),
    visibility varchar(32) NOT NULL DEFAULT 'Private' CHECK (visibility IN ('Private','Workspace','Public')),
    created_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE (workspace_id, name),
    UNIQUE (account_id, workspace_id, id)
);

CREATE TABLE IF NOT EXISTS reporting.dashboard_widgets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    dashboard_id uuid NOT NULL,
    widget_type varchar(80) NOT NULL,
    title varchar(160) NOT NULL,
    position_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    FOREIGN KEY (account_id, workspace_id, dashboard_id) REFERENCES reporting.dashboards(account_id, workspace_id, id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS reporting.dashboard_sources (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    dashboard_id uuid NOT NULL REFERENCES reporting.dashboards(id) ON DELETE CASCADE,
    source_resource_type varchar(80) NOT NULL,
    source_resource_id uuid NOT NULL,
    config_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (dashboard_id, source_resource_type, source_resource_id)
);

CREATE TABLE IF NOT EXISTS reporting.reporting_snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    report_type varchar(120) NOT NULL,
    period_start timestamptz NOT NULL,
    period_end timestamptz NOT NULL,
    snapshot_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    captured_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, report_type, period_start, period_end),
    CHECK (period_end > period_start)
);


-- SEARCH TABLES
CREATE TABLE IF NOT EXISTS search.search_documents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    title text NOT NULL,
    content text NOT NULL DEFAULT '',
    tags text[] NOT NULL DEFAULT '{}'::text[],
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    search_vector tsvector GENERATED ALWAYS AS (to_tsvector('simple', coalesce(title,'') || ' ' || coalesce(content,''))) STORED,
    source_version bigint NOT NULL DEFAULT 1,
    indexed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, resource_type, resource_id)
);

CREATE TABLE IF NOT EXISTS search.search_index_jobs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    resource_type varchar(80) NOT NULL,
    resource_id uuid NOT NULL,
    operation varchar(32) NOT NULL CHECK (operation IN ('Upsert','Delete','Rebuild')),
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','DeadLettered')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz NOT NULL DEFAULT now(),
    locked_by varchar(120),
    locked_until timestamptz,
    last_error text,
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);


-- NOTIFICATIONS TABLES
CREATE TABLE IF NOT EXISTS notifications.notification_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    notification_type varchar(120) NOT NULL,
    title varchar(255) NOT NULL,
    body text,
    actor_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    resource_type varchar(80),
    resource_id uuid,
    dedupe_key varchar(200),
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, dedupe_key)
);

CREATE TABLE IF NOT EXISTS notifications.notification_recipients (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    notification_id uuid NOT NULL REFERENCES notifications.notification_items(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    state varchar(32) NOT NULL DEFAULT 'Unread' CHECK (state IN ('Unread','Read','Archived','Deleted')),
    read_at timestamptz,
    archived_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (notification_id, user_id)
);

CREATE TABLE IF NOT EXISTS notifications.notification_preferences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    notification_type varchar(120) NOT NULL,
    channel varchar(40) NOT NULL CHECK (channel IN ('InApp','Email','Push','Webhook')),
    enabled boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, user_id, notification_type, channel)
);

CREATE TABLE IF NOT EXISTS notifications.notification_deliveries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    notification_recipient_id uuid NOT NULL REFERENCES notifications.notification_recipients(id) ON DELETE CASCADE,
    channel varchar(40) NOT NULL CHECK (channel IN ('InApp','Email','Push','Webhook')),
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Sending','Succeeded','Failed','Skipped','DeadLettered')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz,
    last_error text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS notifications.notification_counters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    unread_count integer NOT NULL DEFAULT 0 CHECK (unread_count >= 0),
    last_notification_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, workspace_id, user_id)
);

CREATE TABLE IF NOT EXISTS notifications.email_outbox (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    to_email citext NOT NULL,
    template_key varchar(120) NOT NULL,
    subject varchar(255) NOT NULL,
    body_text text,
    body_html text,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Sending','Sent','Failed','DeadLettered','Cancelled')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz NOT NULL DEFAULT now(),
    dedupe_key varchar(200),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, dedupe_key)
);

CREATE TABLE IF NOT EXISTS notifications.email_delivery_attempts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email_outbox_id uuid NOT NULL REFERENCES notifications.email_outbox(id) ON DELETE CASCADE,
    provider varchar(80),
    provider_message_id varchar(200),
    status varchar(32) NOT NULL CHECK (status IN ('Succeeded','Failed')),
    status_code varchar(80),
    error_message text,
    attempted_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now()
);


-- ACTIVITY TABLES
CREATE TABLE IF NOT EXISTS activity.workspace_activity_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    activity_type varchar(120) NOT NULL,
    actor_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    subject_resource_type varchar(80),
    subject_resource_id uuid,
    target_resource_type varchar(80),
    target_resource_id uuid,
    visibility varchar(32) NOT NULL DEFAULT 'Workspace' CHECK (visibility IN ('Private','Workspace','Public')),
    importance varchar(32) NOT NULL DEFAULT 'Normal' CHECK (importance IN ('Low','Normal','High','Critical')),
    source_event_id uuid,
    source_message_id uuid,
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS activity.activity_read_states (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    last_read_activity_id uuid,
    last_read_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, user_id)
);


-- ANALYTICS TABLES
CREATE TABLE IF NOT EXISTS analytics.workspace_usage_daily (
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    usage_date date NOT NULL,
    active_users integer NOT NULL DEFAULT 0 CHECK (active_users >= 0),
    items_created integer NOT NULL DEFAULT 0 CHECK (items_created >= 0),
    comments_created integer NOT NULL DEFAULT 0 CHECK (comments_created >= 0),
    automation_runs integer NOT NULL DEFAULT 0 CHECK (automation_runs >= 0),
    metrics_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (workspace_id, usage_date)
);

CREATE TABLE IF NOT EXISTS analytics.feature_usage_daily (
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    feature_code varchar(120) NOT NULL,
    usage_date date NOT NULL,
    quantity bigint NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    metrics_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (account_id, workspace_id, feature_code, usage_date)
);


-- EVENTS TABLES
CREATE TABLE IF NOT EXISTS events.domain_event_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id uuid NOT NULL UNIQUE,
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE SET NULL,
    event_name varchar(200) NOT NULL,
    event_version integer NOT NULL DEFAULT 1 CHECK (event_version > 0),
    source_context varchar(80) NOT NULL,
    aggregate_type varchar(120),
    aggregate_id uuid,
    subject_type varchar(120),
    subject_id uuid,
    actor_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    correlation_id uuid,
    causation_id uuid,
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);


-- MESSAGING TABLES
CREATE TABLE IF NOT EXISTS messaging.outbox_messages (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id uuid NOT NULL UNIQUE,
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE SET NULL,
    message_name varchar(200) NOT NULL,
    schema_version integer NOT NULL DEFAULT 1 CHECK (schema_version > 0),
    source_context varchar(80) NOT NULL,
    destination varchar(160),
    aggregate_type varchar(120),
    aggregate_id uuid,
    subject_type varchar(120),
    subject_id uuid,
    actor_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    correlation_id uuid,
    causation_id uuid,
    partition_key varchar(200),
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    headers_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Publishing','Published','Failed','DeadLettered')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz NOT NULL DEFAULT now(),
    locked_by varchar(120),
    locked_until timestamptz,
    last_error text,
    occurred_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS messaging.outbox_delivery_attempts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    outbox_message_id uuid NOT NULL REFERENCES messaging.outbox_messages(id) ON DELETE CASCADE,
    attempt_number integer NOT NULL CHECK (attempt_number > 0),
    status varchar(32) NOT NULL CHECK (status IN ('Succeeded','Failed')),
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    error_message text,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (outbox_message_id, attempt_number)
);

CREATE TABLE IF NOT EXISTS messaging.processed_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id uuid NOT NULL,
    consumer_name varchar(200) NOT NULL,
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE SET NULL,
    processed_at timestamptz NOT NULL DEFAULT now(),
    source_message_id uuid,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (event_id, consumer_name)
);


-- AUDIT TABLES
CREATE TABLE IF NOT EXISTS audit.audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE SET NULL,
    actor_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    action varchar(255) NOT NULL,
    resource_type varchar(80),
    resource_id uuid,
    result varchar(32) NOT NULL DEFAULT 'Succeeded' CHECK (result IN ('Succeeded','Failed','Denied')),
    ip_address inet,
    user_agent text,
    before_json jsonb,
    after_json jsonb,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    correlation_id uuid,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS audit.security_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE SET NULL,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE SET NULL,
    user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    event_type varchar(160) NOT NULL,
    severity varchar(32) NOT NULL DEFAULT 'Info' CHECK (severity IN ('Info','Low','Medium','High','Critical')),
    risk_score integer NOT NULL DEFAULT 0 CHECK (risk_score BETWEEN 0 AND 100),
    ip_address inet,
    user_agent text,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now()
);


-- OPS TABLES
CREATE TABLE IF NOT EXISTS ops.idempotency_keys (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    scope varchar(120) NOT NULL,
    idempotency_key varchar(200) NOT NULL,
    request_hash varchar(128) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Processing' CHECK (status IN ('Processing','Succeeded','Failed')),
    response_status_code integer,
    response_body_json jsonb,
    locked_until timestamptz,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (scope, idempotency_key)
);

CREATE TABLE IF NOT EXISTS ops.job_locks (
    lock_key varchar(200) PRIMARY KEY,
    owner_id varchar(120) NOT NULL,
    fencing_token bigint NOT NULL DEFAULT 1 CHECK (fencing_token > 0),
    locked_until timestamptz NOT NULL,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ops.import_jobs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type varchar(120) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled')),
    requested_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    source_file_key varchar(500),
    result_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    error_message text,
    started_at timestamptz,
    finished_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ops.export_jobs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES account.accounts(id) ON DELETE CASCADE,
    workspace_id uuid REFERENCES workspace.workspaces(id) ON DELETE CASCADE,
    job_type varchar(120) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Running','Succeeded','Failed','Cancelled')),
    requested_by_user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    result_file_key varchar(500),
    result_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    error_message text,
    started_at timestamptz,
    finished_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ops.cleanup_runs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cleanup_type varchar(120) NOT NULL,
    status varchar(32) NOT NULL DEFAULT 'Running' CHECK (status IN ('Running','Succeeded','Failed')),
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    deleted_rows bigint NOT NULL DEFAULT 0 CHECK (deleted_rows >= 0),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    error_message text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

-- SECTION 6: RLS ACCESS FUNCTIONS
CREATE OR REPLACE FUNCTION authz.can_access_account(p_account_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT authz.is_worker_scope() OR EXISTS (
        SELECT 1
        FROM authz.access_grants g
        WHERE g.account_id = p_account_id
          AND g.user_id = authz.current_user_id()
          AND g.revoked_at IS NULL
          AND (g.expires_at IS NULL OR g.expires_at > now())
    );
$$;

CREATE OR REPLACE FUNCTION authz.can_access_workspace(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, pg_temp
AS $$
    SELECT authz.is_worker_scope() OR EXISTS (
        SELECT 1
        FROM authz.access_grants g
        WHERE g.account_id = p_account_id
          AND g.user_id = authz.current_user_id()
          AND (g.workspace_id IS NULL OR g.workspace_id = p_workspace_id)
          AND g.revoked_at IS NULL
          AND (g.expires_at IS NULL OR g.expires_at > now())
    );
$$;

-- SECTION 7: INDEXES
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_invitations_pending_email ON account.account_invitations(account_id, email) WHERE status = 'Pending';
CREATE UNIQUE INDEX IF NOT EXISTS ux_workspace_invitations_pending_email ON workspace.workspace_invitations(workspace_id, email) WHERE status = 'Pending';
CREATE UNIQUE INDEX IF NOT EXISTS ux_identity_mfa_primary_active ON identity.user_mfa_methods(user_id) WHERE is_primary AND enabled AND deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_billing_payment_method_default ON billing.payment_methods(account_id) WHERE is_default AND deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_work_board_default_group ON work.board_groups(board_id) WHERE is_default AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_search_documents_vector ON search.search_documents USING gin(search_vector);
CREATE INDEX IF NOT EXISTS ix_search_documents_title_trgm ON search.search_documents USING gin(title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_search_documents_content_trgm ON search.search_documents USING gin(content gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_pending ON messaging.outbox_messages(status, next_attempt_at) WHERE status IN ('Pending','Failed');
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_locked ON messaging.outbox_messages(locked_until) WHERE locked_until IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_notifications_email_pending ON notifications.email_outbox(status, next_attempt_at) WHERE status IN ('Pending','Failed');
CREATE INDEX IF NOT EXISTS ix_search_index_jobs_pending ON search.search_index_jobs(status, next_attempt_at) WHERE status IN ('Pending','Failed');
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_expiry ON ops.idempotency_keys(expires_at);
CREATE INDEX IF NOT EXISTS ix_ops_job_locks_expiry ON ops.job_locks(locked_until);
CREATE INDEX IF NOT EXISTS ix_account_accounts_active ON account.accounts(updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_account_accounts_settings_json_gin ON account.accounts USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_account_accounts_metadata_json_gin ON account.accounts USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_account_accounts_created_at ON account.accounts(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_users_active ON identity.users(updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_identity_users_metadata_json_gin ON identity.users USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_identity_users_created_at ON identity.users(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_user_profiles_preferences_json_gin ON identity.user_profiles USING gin(preferences_json);
CREATE INDEX IF NOT EXISTS ix_identity_user_profiles_created_at ON identity.user_profiles(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_user_sessions_created_at ON identity.user_sessions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_oauth_accounts_metadata_json_gin ON identity.oauth_accounts USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_identity_oauth_accounts_created_at ON identity.oauth_accounts(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_user_security_settings_settings_json_gin ON identity.user_security_settings USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_identity_user_security_settings_created_at ON identity.user_security_settings(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_user_mfa_methods_active ON identity.user_mfa_methods(updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_identity_user_mfa_methods_created_at ON identity.user_mfa_methods(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_email_verification_tokens_created_at ON identity.email_verification_tokens(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_password_reset_tokens_created_at ON identity.password_reset_tokens(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_identity_user_api_tokens_account ON identity.user_api_tokens(account_id);
CREATE INDEX IF NOT EXISTS ix_identity_user_api_tokens_created_at ON identity.user_api_tokens(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_members_account ON account.account_members(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_members_active_account ON account.account_members(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_account_account_members_created_at ON account.account_members(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_invitations_account ON account.account_invitations(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_invitations_created_at ON account.account_invitations(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_domains_account ON account.account_domains(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_domains_created_at ON account.account_domains(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_settings_account ON account.account_settings(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_settings_setting_value_gin ON account.account_settings USING gin(setting_value);
CREATE INDEX IF NOT EXISTS ix_account_account_settings_created_at ON account.account_settings(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_regions_account ON account.account_regions(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_regions_created_at ON account.account_regions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_account_identity_providers_account ON account.account_identity_providers(account_id);
CREATE INDEX IF NOT EXISTS ix_account_account_identity_providers_active_account ON account.account_identity_providers(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_account_account_identity_providers_config_json_gin ON account.account_identity_providers USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_account_account_identity_providers_created_at ON account.account_identity_providers(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_scim_directories_account ON account.scim_directories(account_id);
CREATE INDEX IF NOT EXISTS ix_account_scim_directories_active_account ON account.scim_directories(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_account_scim_directories_config_json_gin ON account.scim_directories USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_account_scim_directories_created_at ON account.scim_directories(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_scim_sync_runs_account ON account.scim_sync_runs(account_id);
CREATE INDEX IF NOT EXISTS ix_account_scim_sync_runs_metadata_json_gin ON account.scim_sync_runs USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_account_scim_sync_runs_created_at ON account.scim_sync_runs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_account_workspace_routes_account ON account.workspace_routes(account_id);
CREATE INDEX IF NOT EXISTS ix_account_workspace_routes_created_at ON account.workspace_routes(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_account ON workspace.workspaces(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_active_ws ON workspace.workspaces(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_settings_json_gin ON workspace.workspaces USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_workspace_workspaces_created_at ON workspace.workspaces(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_members_account ON workspace.workspace_members(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_members_workspace ON workspace.workspace_members(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_members_active_ws ON workspace.workspace_members(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_members_created_at ON workspace.workspace_members(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_invitations_account ON workspace.workspace_invitations(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_invitations_workspace ON workspace.workspace_invitations(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_workspace_invitations_created_at ON workspace.workspace_invitations(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_account ON workspace.spaces(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_workspace ON workspace.spaces(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_active_ws ON workspace.spaces(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_workspace_spaces_created_at ON workspace.spaces(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_teams_account ON workspace.teams(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_teams_workspace ON workspace.teams(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_teams_active_ws ON workspace.teams(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_workspace_teams_created_at ON workspace.teams(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_team_members_account ON workspace.team_members(account_id);
CREATE INDEX IF NOT EXISTS ix_workspace_team_members_workspace ON workspace.team_members(workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_team_members_active_ws ON workspace.team_members(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_workspace_team_members_created_at ON workspace.team_members(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_custom_roles_account ON governance.custom_roles(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_custom_roles_workspace ON governance.custom_roles(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_custom_roles_active_ws ON governance.custom_roles(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_custom_roles_created_at ON governance.custom_roles(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_custom_role_permissions_account ON governance.custom_role_permissions(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_custom_role_permissions_workspace ON governance.custom_role_permissions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_custom_role_permissions_created_at ON governance.custom_role_permissions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_member_role_assignments_account ON governance.workspace_member_role_assignments(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_member_role_assignments_workspace ON governance.workspace_member_role_assignments(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_member_role_assignments_created_at ON governance.workspace_member_role_assignments(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_account ON governance.resource_permissions(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_workspace ON governance.resource_permissions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_active_ws ON governance.resource_permissions(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_metadata_json_gin ON governance.resource_permissions USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permissions_created_at ON governance.resource_permissions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_field_permissions_account ON governance.field_permissions(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_field_permissions_workspace ON governance.field_permissions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_field_permissions_active_ws ON governance.field_permissions(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_field_permissions_created_at ON governance.field_permissions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_account ON governance.permission_rules(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_workspace ON governance.permission_rules(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_active_ws ON governance.permission_rules(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_condition_json_gin ON governance.permission_rules USING gin(condition_json);
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_effect_json_gin ON governance.permission_rules USING gin(effect_json);
CREATE INDEX IF NOT EXISTS ix_governance_permission_rules_created_at ON governance.permission_rules(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_permission_templates_account ON governance.permission_templates(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_permission_templates_workspace ON governance.permission_templates(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_permission_templates_active_ws ON governance.permission_templates(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_permission_templates_template_json_gin ON governance.permission_templates USING gin(template_json);
CREATE INDEX IF NOT EXISTS ix_governance_permission_templates_created_at ON governance.permission_templates(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_policies_account ON governance.workspace_policies(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_policies_workspace ON governance.workspace_policies(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_policies_policy_value_gin ON governance.workspace_policies USING gin(policy_value);
CREATE INDEX IF NOT EXISTS ix_governance_workspace_policies_created_at ON governance.workspace_policies(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_share_links_account ON governance.share_links(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_share_links_workspace ON governance.share_links(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_share_links_active_ws ON governance.share_links(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_share_links_created_at ON governance.share_links(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permission_inheritance_cache_account ON governance.resource_permission_inheritance_cache(account_id);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permission_inheritance_cache_workspace ON governance.resource_permission_inheritance_cache(workspace_id);
CREATE INDEX IF NOT EXISTS ix_governance_resource_permission_inheritance_cache_created_at ON governance.resource_permission_inheritance_cache(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_authz_access_grants_account ON authz.access_grants(account_id);
CREATE INDEX IF NOT EXISTS ix_authz_access_grants_workspace ON authz.access_grants(workspace_id);
CREATE INDEX IF NOT EXISTS ix_authz_access_grants_created_at ON authz.access_grants(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_boards_account ON work.boards(account_id);
CREATE INDEX IF NOT EXISTS ix_work_boards_workspace ON work.boards(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_boards_active_ws ON work.boards(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_boards_settings_json_gin ON work.boards USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_work_boards_created_at ON work.boards(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_groups_account ON work.board_groups(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_groups_workspace ON work.board_groups(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_groups_active_ws ON work.board_groups(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_groups_created_at ON work.board_groups(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_fields_account ON work.board_fields(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_fields_workspace ON work.board_fields(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_fields_active_ws ON work.board_fields(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_fields_settings_json_gin ON work.board_fields USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_work_board_fields_created_at ON work.board_fields(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_field_options_account ON work.field_options(account_id);
CREATE INDEX IF NOT EXISTS ix_work_field_options_workspace ON work.field_options(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_field_options_created_at ON work.field_options(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_items_account ON work.board_items(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_items_workspace ON work.board_items(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_items_active_ws ON work.board_items(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_items_created_at ON work.board_items(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_account ON work.board_item_values(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_workspace ON work.board_item_values(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_value_json_gin ON work.board_item_values USING gin(value_json);
CREATE INDEX IF NOT EXISTS ix_work_board_item_values_created_at ON work.board_item_values(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_item_members_account ON work.board_item_members(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_members_workspace ON work.board_item_members(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_members_created_at ON work.board_item_members(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_labels_account ON work.labels(account_id);
CREATE INDEX IF NOT EXISTS ix_work_labels_workspace ON work.labels(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_labels_active_ws ON work.labels(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_labels_created_at ON work.labels(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_item_labels_account ON work.board_item_labels(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_labels_workspace ON work.board_item_labels(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_labels_created_at ON work.board_item_labels(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_views_account ON work.board_views(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_views_workspace ON work.board_views(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_views_active_ws ON work.board_views(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_views_config_json_gin ON work.board_views USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_work_board_views_created_at ON work.board_views(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_view_user_preferences_account ON work.board_view_user_preferences(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_view_user_preferences_workspace ON work.board_view_user_preferences(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_view_user_preferences_preferences_json_gin ON work.board_view_user_preferences USING gin(preferences_json);
CREATE INDEX IF NOT EXISTS ix_work_board_view_user_preferences_created_at ON work.board_view_user_preferences(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_saved_filters_account ON work.saved_filters(account_id);
CREATE INDEX IF NOT EXISTS ix_work_saved_filters_workspace ON work.saved_filters(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_saved_filters_active_ws ON work.saved_filters(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_saved_filters_filter_json_gin ON work.saved_filters USING gin(filter_json);
CREATE INDEX IF NOT EXISTS ix_work_saved_filters_created_at ON work.saved_filters(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_view_pins_account ON work.board_view_pins(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_view_pins_workspace ON work.board_view_pins(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_view_pins_created_at ON work.board_view_pins(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_item_links_account ON work.board_item_links(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_links_workspace ON work.board_item_links(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_links_active_ws ON work.board_item_links(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_item_links_metadata_json_gin ON work.board_item_links USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_work_board_item_links_created_at ON work.board_item_links(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_checklists_account ON work.checklists(account_id);
CREATE INDEX IF NOT EXISTS ix_work_checklists_workspace ON work.checklists(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_checklists_active_ws ON work.checklists(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_checklists_created_at ON work.checklists(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_checklist_items_account ON work.checklist_items(account_id);
CREATE INDEX IF NOT EXISTS ix_work_checklist_items_workspace ON work.checklist_items(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_checklist_items_active_ws ON work.checklist_items(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_checklist_items_created_at ON work.checklist_items(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_relation_field_configs_account ON work.relation_field_configs(account_id);
CREATE INDEX IF NOT EXISTS ix_work_relation_field_configs_workspace ON work.relation_field_configs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_relation_field_configs_config_json_gin ON work.relation_field_configs USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_work_relation_field_configs_created_at ON work.relation_field_configs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_relations_account ON work.board_relations(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_relations_workspace ON work.board_relations(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_relations_active_ws ON work.board_relations(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_relations_created_at ON work.board_relations(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_account ON work.board_item_connections(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_workspace ON work.board_item_connections(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_active_ws ON work.board_item_connections(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_metadata_json_gin ON work.board_item_connections USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_work_board_item_connections_created_at ON work.board_item_connections(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_formula_dependencies_account ON work.formula_dependencies(account_id);
CREATE INDEX IF NOT EXISTS ix_work_formula_dependencies_workspace ON work.formula_dependencies(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_formula_dependencies_created_at ON work.formula_dependencies(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_mirror_value_snapshots_account ON work.mirror_value_snapshots(account_id);
CREATE INDEX IF NOT EXISTS ix_work_mirror_value_snapshots_workspace ON work.mirror_value_snapshots(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_mirror_value_snapshots_value_json_gin ON work.mirror_value_snapshots USING gin(value_json);
CREATE INDEX IF NOT EXISTS ix_work_mirror_value_snapshots_created_at ON work.mirror_value_snapshots(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_rollup_snapshots_account ON work.rollup_snapshots(account_id);
CREATE INDEX IF NOT EXISTS ix_work_rollup_snapshots_workspace ON work.rollup_snapshots(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_rollup_snapshots_value_json_gin ON work.rollup_snapshots USING gin(value_json);
CREATE INDEX IF NOT EXISTS ix_work_rollup_snapshots_created_at ON work.rollup_snapshots(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_approval_requests_account ON work.approval_requests(account_id);
CREATE INDEX IF NOT EXISTS ix_work_approval_requests_workspace ON work.approval_requests(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_approval_requests_active_ws ON work.approval_requests(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_approval_requests_created_at ON work.approval_requests(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_approval_steps_account ON work.approval_steps(account_id);
CREATE INDEX IF NOT EXISTS ix_work_approval_steps_workspace ON work.approval_steps(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_approval_steps_created_at ON work.approval_steps(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_workload_allocations_account ON work.workload_allocations(account_id);
CREATE INDEX IF NOT EXISTS ix_work_workload_allocations_workspace ON work.workload_allocations(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_workload_allocations_created_at ON work.workload_allocations(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_templates_account ON work.board_templates(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_templates_workspace ON work.board_templates(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_templates_active_ws ON work.board_templates(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_board_templates_template_json_gin ON work.board_templates USING gin(template_json);
CREATE INDEX IF NOT EXISTS ix_work_board_templates_created_at ON work.board_templates(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_item_templates_account ON work.item_templates(account_id);
CREATE INDEX IF NOT EXISTS ix_work_item_templates_workspace ON work.item_templates(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_item_templates_active_ws ON work.item_templates(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_item_templates_template_json_gin ON work.item_templates USING gin(template_json);
CREATE INDEX IF NOT EXISTS ix_work_item_templates_created_at ON work.item_templates(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_board_subscribers_account ON work.board_subscribers(account_id);
CREATE INDEX IF NOT EXISTS ix_work_board_subscribers_workspace ON work.board_subscribers(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_board_subscribers_created_at ON work.board_subscribers(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_item_dependencies_account ON work.item_dependencies(account_id);
CREATE INDEX IF NOT EXISTS ix_work_item_dependencies_workspace ON work.item_dependencies(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_item_dependencies_active_ws ON work.item_dependencies(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_item_dependencies_created_at ON work.item_dependencies(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_account ON work.time_tracking_entries(account_id);
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_workspace ON work.time_tracking_entries(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_active_ws ON work.time_tracking_entries(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_time_tracking_entries_created_at ON work.time_tracking_entries(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_forms_account ON work.forms(account_id);
CREATE INDEX IF NOT EXISTS ix_work_forms_workspace ON work.forms(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_forms_active_ws ON work.forms(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_forms_settings_json_gin ON work.forms USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_work_forms_created_at ON work.forms(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_form_questions_account ON work.form_questions(account_id);
CREATE INDEX IF NOT EXISTS ix_work_form_questions_workspace ON work.form_questions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_form_questions_active_ws ON work.form_questions(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_work_form_questions_settings_json_gin ON work.form_questions USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_work_form_questions_created_at ON work.form_questions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_account ON work.form_submissions(account_id);
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_workspace ON work.form_submissions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_answers_json_gin ON work.form_submissions USING gin(answers_json);
CREATE INDEX IF NOT EXISTS ix_work_form_submissions_created_at ON work.form_submissions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_docs_pages_account ON docs.pages(account_id);
CREATE INDEX IF NOT EXISTS ix_docs_pages_workspace ON docs.pages(workspace_id);
CREATE INDEX IF NOT EXISTS ix_docs_pages_active_ws ON docs.pages(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_docs_pages_created_at ON docs.pages(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_docs_blocks_account ON docs.blocks(account_id);
CREATE INDEX IF NOT EXISTS ix_docs_blocks_workspace ON docs.blocks(workspace_id);
CREATE INDEX IF NOT EXISTS ix_docs_blocks_active_ws ON docs.blocks(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_docs_blocks_content_json_gin ON docs.blocks USING gin(content_json);
CREATE INDEX IF NOT EXISTS ix_docs_blocks_created_at ON docs.blocks(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_docs_document_versions_account ON docs.document_versions(account_id);
CREATE INDEX IF NOT EXISTS ix_docs_document_versions_workspace ON docs.document_versions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_docs_document_versions_snapshot_json_gin ON docs.document_versions USING gin(snapshot_json);
CREATE INDEX IF NOT EXISTS ix_docs_document_versions_created_at ON docs.document_versions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_docs_resource_links_account ON docs.resource_links(account_id);
CREATE INDEX IF NOT EXISTS ix_docs_resource_links_workspace ON docs.resource_links(workspace_id);
CREATE INDEX IF NOT EXISTS ix_docs_resource_links_created_at ON docs.resource_links(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_docs_page_templates_account ON docs.page_templates(account_id);
CREATE INDEX IF NOT EXISTS ix_docs_page_templates_workspace ON docs.page_templates(workspace_id);
CREATE INDEX IF NOT EXISTS ix_docs_page_templates_active_ws ON docs.page_templates(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_docs_page_templates_template_json_gin ON docs.page_templates USING gin(template_json);
CREATE INDEX IF NOT EXISTS ix_docs_page_templates_created_at ON docs.page_templates(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_comments_account ON collab.comments(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_comments_workspace ON collab.comments(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_comments_active_ws ON collab.comments(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_collab_comments_created_at ON collab.comments(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_reactions_account ON collab.reactions(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_reactions_workspace ON collab.reactions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_reactions_created_at ON collab.reactions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_mentions_account ON collab.mentions(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_mentions_workspace ON collab.mentions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_mentions_created_at ON collab.mentions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_attachments_account ON collab.attachments(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_attachments_workspace ON collab.attachments(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_attachments_active_ws ON collab.attachments(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_collab_attachments_created_at ON collab.attachments(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_resource_watchers_account ON collab.resource_watchers(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_resource_watchers_workspace ON collab.resource_watchers(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_resource_watchers_created_at ON collab.resource_watchers(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_presence_sessions_account ON collab.presence_sessions(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_presence_sessions_workspace ON collab.presence_sessions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_presence_sessions_created_at ON collab.presence_sessions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_collab_resource_read_states_account ON collab.resource_read_states(account_id);
CREATE INDEX IF NOT EXISTS ix_collab_resource_read_states_workspace ON collab.resource_read_states(workspace_id);
CREATE INDEX IF NOT EXISTS ix_collab_resource_read_states_created_at ON collab.resource_read_states(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_account ON automation.automation_rules(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_workspace ON automation.automation_rules(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_active_ws ON automation.automation_rules(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_trigger_json_gin ON automation.automation_rules USING gin(trigger_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_conditions_json_gin ON automation.automation_rules USING gin(conditions_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_actions_json_gin ON automation.automation_rules USING gin(actions_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_rules_created_at ON automation.automation_rules(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_automation_executions_account ON automation.automation_executions(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_executions_workspace ON automation.automation_executions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_executions_trigger_payload_json_gin ON automation.automation_executions USING gin(trigger_payload_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_executions_result_json_gin ON automation.automation_executions USING gin(result_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_executions_created_at ON automation.automation_executions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_scheduled_jobs_account ON automation.scheduled_jobs(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_scheduled_jobs_workspace ON automation.scheduled_jobs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_scheduled_jobs_payload_json_gin ON automation.scheduled_jobs USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_automation_scheduled_jobs_created_at ON automation.scheduled_jobs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_automation_templates_account ON automation.automation_templates(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_templates_workspace ON automation.automation_templates(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_automation_templates_active_ws ON automation.automation_templates(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_automation_automation_templates_template_json_gin ON automation.automation_templates USING gin(template_json);
CREATE INDEX IF NOT EXISTS ix_automation_automation_templates_created_at ON automation.automation_templates(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_account ON automation.ai_agents(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_workspace ON automation.ai_agents(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_active_ws ON automation.ai_agents(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_model_policy_json_gin ON automation.ai_agents USING gin(model_policy_json);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_tool_policy_json_gin ON automation.ai_agents USING gin(tool_policy_json);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agents_created_at ON automation.ai_agents(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_account ON automation.ai_agent_runs(account_id);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_workspace ON automation.ai_agent_runs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_input_json_gin ON automation.ai_agent_runs USING gin(input_json);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_output_json_gin ON automation.ai_agent_runs USING gin(output_json);
CREATE INDEX IF NOT EXISTS ix_automation_ai_agent_runs_created_at ON automation.ai_agent_runs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_account ON integration.integration_connections(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_workspace ON integration.integration_connections(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_active_ws ON integration.integration_connections(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_metadata_json_gin ON integration.integration_connections USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_integration_integration_connections_created_at ON integration.integration_connections(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_integration_scopes_account ON integration.integration_scopes(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_scopes_workspace ON integration.integration_scopes(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_scopes_created_at ON integration.integration_scopes(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_integration_secret_versions_account ON integration.integration_secret_versions(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_secret_versions_workspace ON integration.integration_secret_versions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_secret_versions_created_at ON integration.integration_secret_versions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_subscriptions_account ON integration.webhook_subscriptions(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_subscriptions_workspace ON integration.webhook_subscriptions(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_subscriptions_active_ws ON integration.webhook_subscriptions(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_integration_webhook_subscriptions_created_at ON integration.webhook_subscriptions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_deliveries_account ON integration.webhook_deliveries(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_deliveries_workspace ON integration.webhook_deliveries(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_deliveries_payload_json_gin ON integration.webhook_deliveries USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_integration_webhook_deliveries_created_at ON integration.webhook_deliveries(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_inbound_webhook_events_account ON integration.inbound_webhook_events(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_inbound_webhook_events_workspace ON integration.inbound_webhook_events(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_inbound_webhook_events_payload_json_gin ON integration.inbound_webhook_events USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_integration_inbound_webhook_events_created_at ON integration.inbound_webhook_events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_account ON integration.calendar_integrations(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_workspace ON integration.calendar_integrations(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_active_ws ON integration.calendar_integrations(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_settings_json_gin ON integration.calendar_integrations USING gin(settings_json);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_integrations_created_at ON integration.calendar_integrations(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_event_links_account ON integration.calendar_event_links(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_event_links_workspace ON integration.calendar_event_links(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_calendar_event_links_created_at ON integration.calendar_event_links(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_integration_integration_sync_cursors_account ON integration.integration_sync_cursors(account_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_sync_cursors_workspace ON integration.integration_sync_cursors(workspace_id);
CREATE INDEX IF NOT EXISTS ix_integration_integration_sync_cursors_created_at ON integration.integration_sync_cursors(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_billing_customers_account ON billing.billing_customers(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_billing_customers_active_account ON billing.billing_customers(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_billing_customers_tax_info_json_gin ON billing.billing_customers USING gin(tax_info_json);
CREATE INDEX IF NOT EXISTS ix_billing_billing_customers_created_at ON billing.billing_customers(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_plans_active ON billing.plans(updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_plans_metadata_json_gin ON billing.plans USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_plans_created_at ON billing.plans(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_plan_prices_active ON billing.plan_prices(updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_plan_prices_created_at ON billing.plan_prices(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_plan_limits_limit_json_gin ON billing.plan_limits USING gin(limit_json);
CREATE INDEX IF NOT EXISTS ix_billing_plan_limits_created_at ON billing.plan_limits(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_subscriptions_account ON billing.subscriptions(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_subscriptions_active_account ON billing.subscriptions(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_subscriptions_metadata_json_gin ON billing.subscriptions USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_subscriptions_created_at ON billing.subscriptions(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_subscription_items_account ON billing.subscription_items(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_subscription_items_active_account ON billing.subscription_items(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_subscription_items_created_at ON billing.subscription_items(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_payment_methods_account ON billing.payment_methods(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_payment_methods_active_account ON billing.payment_methods(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_payment_methods_created_at ON billing.payment_methods(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_invoices_account ON billing.invoices(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_invoices_active_account ON billing.invoices(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_invoices_created_at ON billing.invoices(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_invoice_line_items_account ON billing.invoice_line_items(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_invoice_line_items_metadata_json_gin ON billing.invoice_line_items USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_invoice_line_items_created_at ON billing.invoice_line_items(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_entitlements_account ON billing.entitlements(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_entitlements_active_account ON billing.entitlements(account_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_billing_entitlements_metadata_json_gin ON billing.entitlements USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_entitlements_created_at ON billing.entitlements(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_usage_metrics_account ON billing.usage_metrics(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_usage_metrics_created_at ON billing.usage_metrics(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_usage_metric_history_account ON billing.usage_metric_history(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_usage_metric_history_metadata_json_gin ON billing.usage_metric_history USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_usage_metric_history_created_at ON billing.usage_metric_history(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_feature_usage_ledger_account ON billing.feature_usage_ledger(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_feature_usage_ledger_workspace ON billing.feature_usage_ledger(workspace_id);
CREATE INDEX IF NOT EXISTS ix_billing_feature_usage_ledger_metadata_json_gin ON billing.feature_usage_ledger USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_billing_feature_usage_ledger_created_at ON billing.feature_usage_ledger(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_billing_billing_events_account ON billing.billing_events(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_billing_events_payload_json_gin ON billing.billing_events USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_billing_billing_events_created_at ON billing.billing_events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboards_account ON reporting.dashboards(account_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboards_workspace ON reporting.dashboards(workspace_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboards_active_ws ON reporting.dashboards(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_reporting_dashboards_created_at ON reporting.dashboards(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_account ON reporting.dashboard_widgets(account_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_workspace ON reporting.dashboard_widgets(workspace_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_active_ws ON reporting.dashboard_widgets(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_position_json_gin ON reporting.dashboard_widgets USING gin(position_json);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_config_json_gin ON reporting.dashboard_widgets USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_widgets_created_at ON reporting.dashboard_widgets(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_sources_account ON reporting.dashboard_sources(account_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_sources_workspace ON reporting.dashboard_sources(workspace_id);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_sources_config_json_gin ON reporting.dashboard_sources USING gin(config_json);
CREATE INDEX IF NOT EXISTS ix_reporting_dashboard_sources_created_at ON reporting.dashboard_sources(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_reporting_reporting_snapshots_account ON reporting.reporting_snapshots(account_id);
CREATE INDEX IF NOT EXISTS ix_reporting_reporting_snapshots_workspace ON reporting.reporting_snapshots(workspace_id);
CREATE INDEX IF NOT EXISTS ix_reporting_reporting_snapshots_snapshot_json_gin ON reporting.reporting_snapshots USING gin(snapshot_json);
CREATE INDEX IF NOT EXISTS ix_reporting_reporting_snapshots_created_at ON reporting.reporting_snapshots(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_search_search_documents_account ON search.search_documents(account_id);
CREATE INDEX IF NOT EXISTS ix_search_search_documents_workspace ON search.search_documents(workspace_id);
CREATE INDEX IF NOT EXISTS ix_search_search_documents_metadata_json_gin ON search.search_documents USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_search_search_documents_created_at ON search.search_documents(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_search_search_index_jobs_account ON search.search_index_jobs(account_id);
CREATE INDEX IF NOT EXISTS ix_search_search_index_jobs_workspace ON search.search_index_jobs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_search_search_index_jobs_payload_json_gin ON search.search_index_jobs USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_search_search_index_jobs_created_at ON search.search_index_jobs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_items_account ON notifications.notification_items(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_items_workspace ON notifications.notification_items(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_items_payload_json_gin ON notifications.notification_items USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_items_created_at ON notifications.notification_items(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_recipients_account ON notifications.notification_recipients(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_recipients_workspace ON notifications.notification_recipients(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_recipients_created_at ON notifications.notification_recipients(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_preferences_account ON notifications.notification_preferences(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_preferences_workspace ON notifications.notification_preferences(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_preferences_created_at ON notifications.notification_preferences(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_deliveries_account ON notifications.notification_deliveries(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_deliveries_workspace ON notifications.notification_deliveries(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_deliveries_created_at ON notifications.notification_deliveries(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_counters_account ON notifications.notification_counters(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_counters_workspace ON notifications.notification_counters(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_notification_counters_created_at ON notifications.notification_counters(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_email_outbox_account ON notifications.email_outbox(account_id);
CREATE INDEX IF NOT EXISTS ix_notifications_email_outbox_workspace ON notifications.email_outbox(workspace_id);
CREATE INDEX IF NOT EXISTS ix_notifications_email_outbox_metadata_json_gin ON notifications.email_outbox USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_notifications_email_outbox_created_at ON notifications.email_outbox(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_email_delivery_attempts_created_at ON notifications.email_delivery_attempts(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_activity_workspace_activity_logs_account ON activity.workspace_activity_logs(account_id);
CREATE INDEX IF NOT EXISTS ix_activity_workspace_activity_logs_workspace ON activity.workspace_activity_logs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_activity_workspace_activity_logs_active_ws ON activity.workspace_activity_logs(workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_activity_workspace_activity_logs_data_json_gin ON activity.workspace_activity_logs USING gin(data_json);
CREATE INDEX IF NOT EXISTS ix_activity_workspace_activity_logs_created_at ON activity.workspace_activity_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_activity_activity_read_states_account ON activity.activity_read_states(account_id);
CREATE INDEX IF NOT EXISTS ix_activity_activity_read_states_workspace ON activity.activity_read_states(workspace_id);
CREATE INDEX IF NOT EXISTS ix_activity_activity_read_states_created_at ON activity.activity_read_states(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_analytics_workspace_usage_daily_account ON analytics.workspace_usage_daily(account_id);
CREATE INDEX IF NOT EXISTS ix_analytics_workspace_usage_daily_workspace ON analytics.workspace_usage_daily(workspace_id);
CREATE INDEX IF NOT EXISTS ix_analytics_workspace_usage_daily_metrics_json_gin ON analytics.workspace_usage_daily USING gin(metrics_json);
CREATE INDEX IF NOT EXISTS ix_analytics_workspace_usage_daily_created_at ON analytics.workspace_usage_daily(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_analytics_feature_usage_daily_account ON analytics.feature_usage_daily(account_id);
CREATE INDEX IF NOT EXISTS ix_analytics_feature_usage_daily_workspace ON analytics.feature_usage_daily(workspace_id);
CREATE INDEX IF NOT EXISTS ix_analytics_feature_usage_daily_metrics_json_gin ON analytics.feature_usage_daily USING gin(metrics_json);
CREATE INDEX IF NOT EXISTS ix_analytics_feature_usage_daily_created_at ON analytics.feature_usage_daily(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_events_domain_event_logs_account ON events.domain_event_logs(account_id);
CREATE INDEX IF NOT EXISTS ix_events_domain_event_logs_workspace ON events.domain_event_logs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_events_domain_event_logs_payload_json_gin ON events.domain_event_logs USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_events_domain_event_logs_metadata_json_gin ON events.domain_event_logs USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_events_domain_event_logs_created_at ON events.domain_event_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_messages_account ON messaging.outbox_messages(account_id);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_messages_workspace ON messaging.outbox_messages(workspace_id);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_messages_payload_json_gin ON messaging.outbox_messages USING gin(payload_json);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_messages_headers_json_gin ON messaging.outbox_messages USING gin(headers_json);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_messages_created_at ON messaging.outbox_messages(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_delivery_attempts_metadata_json_gin ON messaging.outbox_delivery_attempts USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_messaging_outbox_delivery_attempts_created_at ON messaging.outbox_delivery_attempts(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_messaging_processed_events_account ON messaging.processed_events(account_id);
CREATE INDEX IF NOT EXISTS ix_messaging_processed_events_workspace ON messaging.processed_events(workspace_id);
CREATE INDEX IF NOT EXISTS ix_messaging_processed_events_metadata_json_gin ON messaging.processed_events USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_messaging_processed_events_created_at ON messaging.processed_events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_account ON audit.audit_logs(account_id);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_workspace ON audit.audit_logs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_before_json_gin ON audit.audit_logs USING gin(before_json);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_after_json_gin ON audit.audit_logs USING gin(after_json);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_metadata_json_gin ON audit.audit_logs USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_audit_audit_logs_created_at ON audit.audit_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_security_events_account ON audit.security_events(account_id);
CREATE INDEX IF NOT EXISTS ix_audit_security_events_workspace ON audit.security_events(workspace_id);
CREATE INDEX IF NOT EXISTS ix_audit_security_events_metadata_json_gin ON audit.security_events USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_audit_security_events_created_at ON audit.security_events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_account ON ops.idempotency_keys(account_id);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_workspace ON ops.idempotency_keys(workspace_id);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_response_body_json_gin ON ops.idempotency_keys USING gin(response_body_json);
CREATE INDEX IF NOT EXISTS ix_ops_idempotency_keys_created_at ON ops.idempotency_keys(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_job_locks_metadata_json_gin ON ops.job_locks USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_ops_job_locks_created_at ON ops.job_locks(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_account ON ops.import_jobs(account_id);
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_workspace ON ops.import_jobs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_result_json_gin ON ops.import_jobs USING gin(result_json);
CREATE INDEX IF NOT EXISTS ix_ops_import_jobs_created_at ON ops.import_jobs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_account ON ops.export_jobs(account_id);
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_workspace ON ops.export_jobs(workspace_id);
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_result_json_gin ON ops.export_jobs USING gin(result_json);
CREATE INDEX IF NOT EXISTS ix_ops_export_jobs_created_at ON ops.export_jobs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_cleanup_runs_metadata_json_gin ON ops.cleanup_runs USING gin(metadata_json);
CREATE INDEX IF NOT EXISTS ix_ops_cleanup_runs_created_at ON ops.cleanup_runs(created_at DESC);

-- SECTION 8: UPDATED_AT TRIGGERS
DROP TRIGGER IF EXISTS trg_account_accounts_set_updated_at ON account.accounts;
CREATE TRIGGER trg_account_accounts_set_updated_at BEFORE UPDATE ON account.accounts FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_users_set_updated_at ON identity.users;
CREATE TRIGGER trg_identity_users_set_updated_at BEFORE UPDATE ON identity.users FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_user_profiles_set_updated_at ON identity.user_profiles;
CREATE TRIGGER trg_identity_user_profiles_set_updated_at BEFORE UPDATE ON identity.user_profiles FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_user_sessions_set_updated_at ON identity.user_sessions;
CREATE TRIGGER trg_identity_user_sessions_set_updated_at BEFORE UPDATE ON identity.user_sessions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_oauth_accounts_set_updated_at ON identity.oauth_accounts;
CREATE TRIGGER trg_identity_oauth_accounts_set_updated_at BEFORE UPDATE ON identity.oauth_accounts FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_user_security_settings_set_updated_at ON identity.user_security_settings;
CREATE TRIGGER trg_identity_user_security_settings_set_updated_at BEFORE UPDATE ON identity.user_security_settings FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_user_mfa_methods_set_updated_at ON identity.user_mfa_methods;
CREATE TRIGGER trg_identity_user_mfa_methods_set_updated_at BEFORE UPDATE ON identity.user_mfa_methods FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_identity_user_api_tokens_set_updated_at ON identity.user_api_tokens;
CREATE TRIGGER trg_identity_user_api_tokens_set_updated_at BEFORE UPDATE ON identity.user_api_tokens FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_members_set_updated_at ON account.account_members;
CREATE TRIGGER trg_account_account_members_set_updated_at BEFORE UPDATE ON account.account_members FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_invitations_set_updated_at ON account.account_invitations;
CREATE TRIGGER trg_account_account_invitations_set_updated_at BEFORE UPDATE ON account.account_invitations FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_domains_set_updated_at ON account.account_domains;
CREATE TRIGGER trg_account_account_domains_set_updated_at BEFORE UPDATE ON account.account_domains FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_settings_set_updated_at ON account.account_settings;
CREATE TRIGGER trg_account_account_settings_set_updated_at BEFORE UPDATE ON account.account_settings FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_regions_set_updated_at ON account.account_regions;
CREATE TRIGGER trg_account_account_regions_set_updated_at BEFORE UPDATE ON account.account_regions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_account_identity_providers_set_updated_at ON account.account_identity_providers;
CREATE TRIGGER trg_account_account_identity_providers_set_updated_at BEFORE UPDATE ON account.account_identity_providers FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_scim_directories_set_updated_at ON account.scim_directories;
CREATE TRIGGER trg_account_scim_directories_set_updated_at BEFORE UPDATE ON account.scim_directories FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_scim_sync_runs_set_updated_at ON account.scim_sync_runs;
CREATE TRIGGER trg_account_scim_sync_runs_set_updated_at BEFORE UPDATE ON account.scim_sync_runs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_account_workspace_routes_set_updated_at ON account.workspace_routes;
CREATE TRIGGER trg_account_workspace_routes_set_updated_at BEFORE UPDATE ON account.workspace_routes FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_workspaces_set_updated_at ON workspace.workspaces;
CREATE TRIGGER trg_workspace_workspaces_set_updated_at BEFORE UPDATE ON workspace.workspaces FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_workspace_members_set_updated_at ON workspace.workspace_members;
CREATE TRIGGER trg_workspace_workspace_members_set_updated_at BEFORE UPDATE ON workspace.workspace_members FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_workspace_invitations_set_updated_at ON workspace.workspace_invitations;
CREATE TRIGGER trg_workspace_workspace_invitations_set_updated_at BEFORE UPDATE ON workspace.workspace_invitations FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_spaces_set_updated_at ON workspace.spaces;
CREATE TRIGGER trg_workspace_spaces_set_updated_at BEFORE UPDATE ON workspace.spaces FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_teams_set_updated_at ON workspace.teams;
CREATE TRIGGER trg_workspace_teams_set_updated_at BEFORE UPDATE ON workspace.teams FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_workspace_team_members_set_updated_at ON workspace.team_members;
CREATE TRIGGER trg_workspace_team_members_set_updated_at BEFORE UPDATE ON workspace.team_members FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_custom_roles_set_updated_at ON governance.custom_roles;
CREATE TRIGGER trg_governance_custom_roles_set_updated_at BEFORE UPDATE ON governance.custom_roles FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_custom_role_permissions_set_updated_at ON governance.custom_role_permissions;
CREATE TRIGGER trg_governance_custom_role_permissions_set_updated_at BEFORE UPDATE ON governance.custom_role_permissions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_workspace_member_role_assignments_set_updated_at ON governance.workspace_member_role_assignments;
CREATE TRIGGER trg_governance_workspace_member_role_assignments_set_updated_at BEFORE UPDATE ON governance.workspace_member_role_assignments FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_resource_permissions_set_updated_at ON governance.resource_permissions;
CREATE TRIGGER trg_governance_resource_permissions_set_updated_at BEFORE UPDATE ON governance.resource_permissions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_field_permissions_set_updated_at ON governance.field_permissions;
CREATE TRIGGER trg_governance_field_permissions_set_updated_at BEFORE UPDATE ON governance.field_permissions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_permission_rules_set_updated_at ON governance.permission_rules;
CREATE TRIGGER trg_governance_permission_rules_set_updated_at BEFORE UPDATE ON governance.permission_rules FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_permission_templates_set_updated_at ON governance.permission_templates;
CREATE TRIGGER trg_governance_permission_templates_set_updated_at BEFORE UPDATE ON governance.permission_templates FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_workspace_policies_set_updated_at ON governance.workspace_policies;
CREATE TRIGGER trg_governance_workspace_policies_set_updated_at BEFORE UPDATE ON governance.workspace_policies FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_share_links_set_updated_at ON governance.share_links;
CREATE TRIGGER trg_governance_share_links_set_updated_at BEFORE UPDATE ON governance.share_links FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_governance_resource_permission_inheritance_cache_set_updated_at ON governance.resource_permission_inheritance_cache;
CREATE TRIGGER trg_governance_resource_permission_inheritance_cache_set_updated_at BEFORE UPDATE ON governance.resource_permission_inheritance_cache FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_authz_access_grants_set_updated_at ON authz.access_grants;
CREATE TRIGGER trg_authz_access_grants_set_updated_at BEFORE UPDATE ON authz.access_grants FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_boards_set_updated_at ON work.boards;
CREATE TRIGGER trg_work_boards_set_updated_at BEFORE UPDATE ON work.boards FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_groups_set_updated_at ON work.board_groups;
CREATE TRIGGER trg_work_board_groups_set_updated_at BEFORE UPDATE ON work.board_groups FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_fields_set_updated_at ON work.board_fields;
CREATE TRIGGER trg_work_board_fields_set_updated_at BEFORE UPDATE ON work.board_fields FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_field_options_set_updated_at ON work.field_options;
CREATE TRIGGER trg_work_field_options_set_updated_at BEFORE UPDATE ON work.field_options FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_items_set_updated_at ON work.board_items;
CREATE TRIGGER trg_work_board_items_set_updated_at BEFORE UPDATE ON work.board_items FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_item_values_set_updated_at ON work.board_item_values;
CREATE TRIGGER trg_work_board_item_values_set_updated_at BEFORE UPDATE ON work.board_item_values FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_labels_set_updated_at ON work.labels;
CREATE TRIGGER trg_work_labels_set_updated_at BEFORE UPDATE ON work.labels FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_views_set_updated_at ON work.board_views;
CREATE TRIGGER trg_work_board_views_set_updated_at BEFORE UPDATE ON work.board_views FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_view_user_preferences_set_updated_at ON work.board_view_user_preferences;
CREATE TRIGGER trg_work_board_view_user_preferences_set_updated_at BEFORE UPDATE ON work.board_view_user_preferences FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_saved_filters_set_updated_at ON work.saved_filters;
CREATE TRIGGER trg_work_saved_filters_set_updated_at BEFORE UPDATE ON work.saved_filters FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_item_links_set_updated_at ON work.board_item_links;
CREATE TRIGGER trg_work_board_item_links_set_updated_at BEFORE UPDATE ON work.board_item_links FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_checklists_set_updated_at ON work.checklists;
CREATE TRIGGER trg_work_checklists_set_updated_at BEFORE UPDATE ON work.checklists FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_checklist_items_set_updated_at ON work.checklist_items;
CREATE TRIGGER trg_work_checklist_items_set_updated_at BEFORE UPDATE ON work.checklist_items FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_relation_field_configs_set_updated_at ON work.relation_field_configs;
CREATE TRIGGER trg_work_relation_field_configs_set_updated_at BEFORE UPDATE ON work.relation_field_configs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_relations_set_updated_at ON work.board_relations;
CREATE TRIGGER trg_work_board_relations_set_updated_at BEFORE UPDATE ON work.board_relations FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_item_connections_set_updated_at ON work.board_item_connections;
CREATE TRIGGER trg_work_board_item_connections_set_updated_at BEFORE UPDATE ON work.board_item_connections FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_mirror_value_snapshots_set_updated_at ON work.mirror_value_snapshots;
CREATE TRIGGER trg_work_mirror_value_snapshots_set_updated_at BEFORE UPDATE ON work.mirror_value_snapshots FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_rollup_snapshots_set_updated_at ON work.rollup_snapshots;
CREATE TRIGGER trg_work_rollup_snapshots_set_updated_at BEFORE UPDATE ON work.rollup_snapshots FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_approval_requests_set_updated_at ON work.approval_requests;
CREATE TRIGGER trg_work_approval_requests_set_updated_at BEFORE UPDATE ON work.approval_requests FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_approval_steps_set_updated_at ON work.approval_steps;
CREATE TRIGGER trg_work_approval_steps_set_updated_at BEFORE UPDATE ON work.approval_steps FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_workload_allocations_set_updated_at ON work.workload_allocations;
CREATE TRIGGER trg_work_workload_allocations_set_updated_at BEFORE UPDATE ON work.workload_allocations FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_board_templates_set_updated_at ON work.board_templates;
CREATE TRIGGER trg_work_board_templates_set_updated_at BEFORE UPDATE ON work.board_templates FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_item_templates_set_updated_at ON work.item_templates;
CREATE TRIGGER trg_work_item_templates_set_updated_at BEFORE UPDATE ON work.item_templates FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_item_dependencies_set_updated_at ON work.item_dependencies;
CREATE TRIGGER trg_work_item_dependencies_set_updated_at BEFORE UPDATE ON work.item_dependencies FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_time_tracking_entries_set_updated_at ON work.time_tracking_entries;
CREATE TRIGGER trg_work_time_tracking_entries_set_updated_at BEFORE UPDATE ON work.time_tracking_entries FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_forms_set_updated_at ON work.forms;
CREATE TRIGGER trg_work_forms_set_updated_at BEFORE UPDATE ON work.forms FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_form_questions_set_updated_at ON work.form_questions;
CREATE TRIGGER trg_work_form_questions_set_updated_at BEFORE UPDATE ON work.form_questions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_work_form_submissions_set_updated_at ON work.form_submissions;
CREATE TRIGGER trg_work_form_submissions_set_updated_at BEFORE UPDATE ON work.form_submissions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_docs_pages_set_updated_at ON docs.pages;
CREATE TRIGGER trg_docs_pages_set_updated_at BEFORE UPDATE ON docs.pages FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_docs_blocks_set_updated_at ON docs.blocks;
CREATE TRIGGER trg_docs_blocks_set_updated_at BEFORE UPDATE ON docs.blocks FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_docs_resource_links_set_updated_at ON docs.resource_links;
CREATE TRIGGER trg_docs_resource_links_set_updated_at BEFORE UPDATE ON docs.resource_links FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_docs_page_templates_set_updated_at ON docs.page_templates;
CREATE TRIGGER trg_docs_page_templates_set_updated_at BEFORE UPDATE ON docs.page_templates FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_collab_comments_set_updated_at ON collab.comments;
CREATE TRIGGER trg_collab_comments_set_updated_at BEFORE UPDATE ON collab.comments FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_collab_mentions_set_updated_at ON collab.mentions;
CREATE TRIGGER trg_collab_mentions_set_updated_at BEFORE UPDATE ON collab.mentions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_collab_attachments_set_updated_at ON collab.attachments;
CREATE TRIGGER trg_collab_attachments_set_updated_at BEFORE UPDATE ON collab.attachments FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_collab_presence_sessions_set_updated_at ON collab.presence_sessions;
CREATE TRIGGER trg_collab_presence_sessions_set_updated_at BEFORE UPDATE ON collab.presence_sessions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_collab_resource_read_states_set_updated_at ON collab.resource_read_states;
CREATE TRIGGER trg_collab_resource_read_states_set_updated_at BEFORE UPDATE ON collab.resource_read_states FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_automation_rules_set_updated_at ON automation.automation_rules;
CREATE TRIGGER trg_automation_automation_rules_set_updated_at BEFORE UPDATE ON automation.automation_rules FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_automation_executions_set_updated_at ON automation.automation_executions;
CREATE TRIGGER trg_automation_automation_executions_set_updated_at BEFORE UPDATE ON automation.automation_executions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_scheduled_jobs_set_updated_at ON automation.scheduled_jobs;
CREATE TRIGGER trg_automation_scheduled_jobs_set_updated_at BEFORE UPDATE ON automation.scheduled_jobs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_automation_templates_set_updated_at ON automation.automation_templates;
CREATE TRIGGER trg_automation_automation_templates_set_updated_at BEFORE UPDATE ON automation.automation_templates FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_ai_agents_set_updated_at ON automation.ai_agents;
CREATE TRIGGER trg_automation_ai_agents_set_updated_at BEFORE UPDATE ON automation.ai_agents FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_automation_ai_agent_runs_set_updated_at ON automation.ai_agent_runs;
CREATE TRIGGER trg_automation_ai_agent_runs_set_updated_at BEFORE UPDATE ON automation.ai_agent_runs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_integration_connections_set_updated_at ON integration.integration_connections;
CREATE TRIGGER trg_integration_integration_connections_set_updated_at BEFORE UPDATE ON integration.integration_connections FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_integration_scopes_set_updated_at ON integration.integration_scopes;
CREATE TRIGGER trg_integration_integration_scopes_set_updated_at BEFORE UPDATE ON integration.integration_scopes FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_integration_secret_versions_set_updated_at ON integration.integration_secret_versions;
CREATE TRIGGER trg_integration_integration_secret_versions_set_updated_at BEFORE UPDATE ON integration.integration_secret_versions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_webhook_subscriptions_set_updated_at ON integration.webhook_subscriptions;
CREATE TRIGGER trg_integration_webhook_subscriptions_set_updated_at BEFORE UPDATE ON integration.webhook_subscriptions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_webhook_deliveries_set_updated_at ON integration.webhook_deliveries;
CREATE TRIGGER trg_integration_webhook_deliveries_set_updated_at BEFORE UPDATE ON integration.webhook_deliveries FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_inbound_webhook_events_set_updated_at ON integration.inbound_webhook_events;
CREATE TRIGGER trg_integration_inbound_webhook_events_set_updated_at BEFORE UPDATE ON integration.inbound_webhook_events FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_calendar_integrations_set_updated_at ON integration.calendar_integrations;
CREATE TRIGGER trg_integration_calendar_integrations_set_updated_at BEFORE UPDATE ON integration.calendar_integrations FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_calendar_event_links_set_updated_at ON integration.calendar_event_links;
CREATE TRIGGER trg_integration_calendar_event_links_set_updated_at BEFORE UPDATE ON integration.calendar_event_links FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_integration_integration_sync_cursors_set_updated_at ON integration.integration_sync_cursors;
CREATE TRIGGER trg_integration_integration_sync_cursors_set_updated_at BEFORE UPDATE ON integration.integration_sync_cursors FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_billing_customers_set_updated_at ON billing.billing_customers;
CREATE TRIGGER trg_billing_billing_customers_set_updated_at BEFORE UPDATE ON billing.billing_customers FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_plans_set_updated_at ON billing.plans;
CREATE TRIGGER trg_billing_plans_set_updated_at BEFORE UPDATE ON billing.plans FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_plan_prices_set_updated_at ON billing.plan_prices;
CREATE TRIGGER trg_billing_plan_prices_set_updated_at BEFORE UPDATE ON billing.plan_prices FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_plan_limits_set_updated_at ON billing.plan_limits;
CREATE TRIGGER trg_billing_plan_limits_set_updated_at BEFORE UPDATE ON billing.plan_limits FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_subscriptions_set_updated_at ON billing.subscriptions;
CREATE TRIGGER trg_billing_subscriptions_set_updated_at BEFORE UPDATE ON billing.subscriptions FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_subscription_items_set_updated_at ON billing.subscription_items;
CREATE TRIGGER trg_billing_subscription_items_set_updated_at BEFORE UPDATE ON billing.subscription_items FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_payment_methods_set_updated_at ON billing.payment_methods;
CREATE TRIGGER trg_billing_payment_methods_set_updated_at BEFORE UPDATE ON billing.payment_methods FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_invoices_set_updated_at ON billing.invoices;
CREATE TRIGGER trg_billing_invoices_set_updated_at BEFORE UPDATE ON billing.invoices FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_invoice_line_items_set_updated_at ON billing.invoice_line_items;
CREATE TRIGGER trg_billing_invoice_line_items_set_updated_at BEFORE UPDATE ON billing.invoice_line_items FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_entitlements_set_updated_at ON billing.entitlements;
CREATE TRIGGER trg_billing_entitlements_set_updated_at BEFORE UPDATE ON billing.entitlements FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_usage_metrics_set_updated_at ON billing.usage_metrics;
CREATE TRIGGER trg_billing_usage_metrics_set_updated_at BEFORE UPDATE ON billing.usage_metrics FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_billing_billing_events_set_updated_at ON billing.billing_events;
CREATE TRIGGER trg_billing_billing_events_set_updated_at BEFORE UPDATE ON billing.billing_events FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_reporting_dashboards_set_updated_at ON reporting.dashboards;
CREATE TRIGGER trg_reporting_dashboards_set_updated_at BEFORE UPDATE ON reporting.dashboards FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_reporting_dashboard_widgets_set_updated_at ON reporting.dashboard_widgets;
CREATE TRIGGER trg_reporting_dashboard_widgets_set_updated_at BEFORE UPDATE ON reporting.dashboard_widgets FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_reporting_dashboard_sources_set_updated_at ON reporting.dashboard_sources;
CREATE TRIGGER trg_reporting_dashboard_sources_set_updated_at BEFORE UPDATE ON reporting.dashboard_sources FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_search_search_documents_set_updated_at ON search.search_documents;
CREATE TRIGGER trg_search_search_documents_set_updated_at BEFORE UPDATE ON search.search_documents FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_search_search_index_jobs_set_updated_at ON search.search_index_jobs;
CREATE TRIGGER trg_search_search_index_jobs_set_updated_at BEFORE UPDATE ON search.search_index_jobs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_notification_items_set_updated_at ON notifications.notification_items;
CREATE TRIGGER trg_notifications_notification_items_set_updated_at BEFORE UPDATE ON notifications.notification_items FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_notification_recipients_set_updated_at ON notifications.notification_recipients;
CREATE TRIGGER trg_notifications_notification_recipients_set_updated_at BEFORE UPDATE ON notifications.notification_recipients FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_notification_preferences_set_updated_at ON notifications.notification_preferences;
CREATE TRIGGER trg_notifications_notification_preferences_set_updated_at BEFORE UPDATE ON notifications.notification_preferences FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_notification_deliveries_set_updated_at ON notifications.notification_deliveries;
CREATE TRIGGER trg_notifications_notification_deliveries_set_updated_at BEFORE UPDATE ON notifications.notification_deliveries FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_notification_counters_set_updated_at ON notifications.notification_counters;
CREATE TRIGGER trg_notifications_notification_counters_set_updated_at BEFORE UPDATE ON notifications.notification_counters FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_notifications_email_outbox_set_updated_at ON notifications.email_outbox;
CREATE TRIGGER trg_notifications_email_outbox_set_updated_at BEFORE UPDATE ON notifications.email_outbox FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_activity_workspace_activity_logs_set_updated_at ON activity.workspace_activity_logs;
CREATE TRIGGER trg_activity_workspace_activity_logs_set_updated_at BEFORE UPDATE ON activity.workspace_activity_logs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_activity_activity_read_states_set_updated_at ON activity.activity_read_states;
CREATE TRIGGER trg_activity_activity_read_states_set_updated_at BEFORE UPDATE ON activity.activity_read_states FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_analytics_workspace_usage_daily_set_updated_at ON analytics.workspace_usage_daily;
CREATE TRIGGER trg_analytics_workspace_usage_daily_set_updated_at BEFORE UPDATE ON analytics.workspace_usage_daily FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_analytics_feature_usage_daily_set_updated_at ON analytics.feature_usage_daily;
CREATE TRIGGER trg_analytics_feature_usage_daily_set_updated_at BEFORE UPDATE ON analytics.feature_usage_daily FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_messaging_outbox_messages_set_updated_at ON messaging.outbox_messages;
CREATE TRIGGER trg_messaging_outbox_messages_set_updated_at BEFORE UPDATE ON messaging.outbox_messages FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_ops_idempotency_keys_set_updated_at ON ops.idempotency_keys;
CREATE TRIGGER trg_ops_idempotency_keys_set_updated_at BEFORE UPDATE ON ops.idempotency_keys FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_ops_job_locks_set_updated_at ON ops.job_locks;
CREATE TRIGGER trg_ops_job_locks_set_updated_at BEFORE UPDATE ON ops.job_locks FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_ops_import_jobs_set_updated_at ON ops.import_jobs;
CREATE TRIGGER trg_ops_import_jobs_set_updated_at BEFORE UPDATE ON ops.import_jobs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_ops_export_jobs_set_updated_at ON ops.export_jobs;
CREATE TRIGGER trg_ops_export_jobs_set_updated_at BEFORE UPDATE ON ops.export_jobs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
DROP TRIGGER IF EXISTS trg_ops_cleanup_runs_set_updated_at ON ops.cleanup_runs;
CREATE TRIGGER trg_ops_cleanup_runs_set_updated_at BEFORE UPDATE ON ops.cleanup_runs FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

-- SECTION 9: RLS AND PRIVILEGES
ALTER TABLE account.accounts ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.accounts TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.accounts TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.accounts TO notrelix_app;
DROP POLICY IF EXISTS account_accounts_worker_all ON account.accounts;
CREATE POLICY account_accounts_worker_all ON account.accounts FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_accounts_support_select ON account.accounts;
CREATE POLICY account_accounts_support_select ON account.accounts FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS account_accounts_app_select ON account.accounts;
CREATE POLICY account_accounts_app_select ON account.accounts FOR SELECT TO notrelix_app USING (true);
DROP POLICY IF EXISTS account_accounts_app_insert ON account.accounts;
CREATE POLICY account_accounts_app_insert ON account.accounts FOR INSERT TO notrelix_app WITH CHECK (true);
DROP POLICY IF EXISTS account_accounts_app_update ON account.accounts;
CREATE POLICY account_accounts_app_update ON account.accounts FOR UPDATE TO notrelix_app USING (true) WITH CHECK (true);

ALTER TABLE identity.users ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.users TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.users TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.users TO notrelix_app;
DROP POLICY IF EXISTS identity_users_worker_all ON identity.users;
CREATE POLICY identity_users_worker_all ON identity.users FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_users_support_select ON identity.users;
CREATE POLICY identity_users_support_select ON identity.users FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_users_app_select ON identity.users;
CREATE POLICY identity_users_app_select ON identity.users FOR SELECT TO notrelix_app USING (id = authz.current_user_id());
DROP POLICY IF EXISTS identity_users_app_update ON identity.users;
CREATE POLICY identity_users_app_update ON identity.users FOR UPDATE TO notrelix_app USING (id = authz.current_user_id()) WITH CHECK (id = authz.current_user_id());

ALTER TABLE identity.user_profiles ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_profiles TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_profiles TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.user_profiles TO notrelix_app;
DROP POLICY IF EXISTS identity_user_profiles_worker_all ON identity.user_profiles;
CREATE POLICY identity_user_profiles_worker_all ON identity.user_profiles FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_profiles_support_select ON identity.user_profiles;
CREATE POLICY identity_user_profiles_support_select ON identity.user_profiles FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_user_profiles_app_select ON identity.user_profiles;
CREATE POLICY identity_user_profiles_app_select ON identity.user_profiles FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());
DROP POLICY IF EXISTS identity_user_profiles_app_update ON identity.user_profiles;
CREATE POLICY identity_user_profiles_app_update ON identity.user_profiles FOR UPDATE TO notrelix_app USING (user_id = authz.current_user_id()) WITH CHECK (user_id = authz.current_user_id());

ALTER TABLE identity.user_sessions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_sessions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_sessions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.user_sessions TO notrelix_app;
DROP POLICY IF EXISTS identity_user_sessions_worker_all ON identity.user_sessions;
CREATE POLICY identity_user_sessions_worker_all ON identity.user_sessions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_sessions_support_select ON identity.user_sessions;
CREATE POLICY identity_user_sessions_support_select ON identity.user_sessions FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_user_sessions_app_select ON identity.user_sessions;
CREATE POLICY identity_user_sessions_app_select ON identity.user_sessions FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());
DROP POLICY IF EXISTS identity_user_sessions_app_update ON identity.user_sessions;
CREATE POLICY identity_user_sessions_app_update ON identity.user_sessions FOR UPDATE TO notrelix_app USING (user_id = authz.current_user_id()) WITH CHECK (user_id = authz.current_user_id());

ALTER TABLE identity.oauth_accounts ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.oauth_accounts TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.oauth_accounts TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.oauth_accounts TO notrelix_app;
DROP POLICY IF EXISTS identity_oauth_accounts_worker_all ON identity.oauth_accounts;
CREATE POLICY identity_oauth_accounts_worker_all ON identity.oauth_accounts FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_oauth_accounts_support_select ON identity.oauth_accounts;
CREATE POLICY identity_oauth_accounts_support_select ON identity.oauth_accounts FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_oauth_accounts_app_select ON identity.oauth_accounts;
CREATE POLICY identity_oauth_accounts_app_select ON identity.oauth_accounts FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());
DROP POLICY IF EXISTS identity_oauth_accounts_app_update ON identity.oauth_accounts;
CREATE POLICY identity_oauth_accounts_app_update ON identity.oauth_accounts FOR UPDATE TO notrelix_app USING (user_id = authz.current_user_id()) WITH CHECK (user_id = authz.current_user_id());

ALTER TABLE identity.user_security_settings ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_security_settings TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_security_settings TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.user_security_settings TO notrelix_app;
DROP POLICY IF EXISTS identity_user_security_settings_worker_all ON identity.user_security_settings;
CREATE POLICY identity_user_security_settings_worker_all ON identity.user_security_settings FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_security_settings_support_select ON identity.user_security_settings;
CREATE POLICY identity_user_security_settings_support_select ON identity.user_security_settings FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_user_security_settings_app_select ON identity.user_security_settings;
CREATE POLICY identity_user_security_settings_app_select ON identity.user_security_settings FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());
DROP POLICY IF EXISTS identity_user_security_settings_app_update ON identity.user_security_settings;
CREATE POLICY identity_user_security_settings_app_update ON identity.user_security_settings FOR UPDATE TO notrelix_app USING (user_id = authz.current_user_id()) WITH CHECK (user_id = authz.current_user_id());

ALTER TABLE identity.user_mfa_methods ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_mfa_methods TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_mfa_methods TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.user_mfa_methods TO notrelix_app;
DROP POLICY IF EXISTS identity_user_mfa_methods_worker_all ON identity.user_mfa_methods;
CREATE POLICY identity_user_mfa_methods_worker_all ON identity.user_mfa_methods FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_mfa_methods_support_select ON identity.user_mfa_methods;
CREATE POLICY identity_user_mfa_methods_support_select ON identity.user_mfa_methods FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_user_mfa_methods_app_select ON identity.user_mfa_methods;
CREATE POLICY identity_user_mfa_methods_app_select ON identity.user_mfa_methods FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());
DROP POLICY IF EXISTS identity_user_mfa_methods_app_update ON identity.user_mfa_methods;
CREATE POLICY identity_user_mfa_methods_app_update ON identity.user_mfa_methods FOR UPDATE TO notrelix_app USING (user_id = authz.current_user_id()) WITH CHECK (user_id = authz.current_user_id());

ALTER TABLE identity.user_login_attempts ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_login_attempts TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_login_attempts TO notrelix_worker;
GRANT SELECT ON identity.user_login_attempts TO notrelix_app;
DROP POLICY IF EXISTS identity_user_login_attempts_worker_all ON identity.user_login_attempts;
CREATE POLICY identity_user_login_attempts_worker_all ON identity.user_login_attempts FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_login_attempts_support_select ON identity.user_login_attempts;
CREATE POLICY identity_user_login_attempts_support_select ON identity.user_login_attempts FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_user_login_attempts_app_select ON identity.user_login_attempts;
CREATE POLICY identity_user_login_attempts_app_select ON identity.user_login_attempts FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());

ALTER TABLE identity.email_verification_tokens ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.email_verification_tokens TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.email_verification_tokens TO notrelix_worker;
GRANT SELECT ON identity.email_verification_tokens TO notrelix_app;
DROP POLICY IF EXISTS identity_email_verification_tokens_worker_all ON identity.email_verification_tokens;
CREATE POLICY identity_email_verification_tokens_worker_all ON identity.email_verification_tokens FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_email_verification_tokens_support_select ON identity.email_verification_tokens;
CREATE POLICY identity_email_verification_tokens_support_select ON identity.email_verification_tokens FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_email_verification_tokens_app_select ON identity.email_verification_tokens;
CREATE POLICY identity_email_verification_tokens_app_select ON identity.email_verification_tokens FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());

ALTER TABLE identity.password_reset_tokens ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.password_reset_tokens TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.password_reset_tokens TO notrelix_worker;
GRANT SELECT ON identity.password_reset_tokens TO notrelix_app;
DROP POLICY IF EXISTS identity_password_reset_tokens_worker_all ON identity.password_reset_tokens;
CREATE POLICY identity_password_reset_tokens_worker_all ON identity.password_reset_tokens FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_password_reset_tokens_support_select ON identity.password_reset_tokens;
CREATE POLICY identity_password_reset_tokens_support_select ON identity.password_reset_tokens FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS identity_password_reset_tokens_app_select ON identity.password_reset_tokens;
CREATE POLICY identity_password_reset_tokens_app_select ON identity.password_reset_tokens FOR SELECT TO notrelix_app USING (user_id = authz.current_user_id());

ALTER TABLE identity.user_api_tokens ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON identity.user_api_tokens TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.user_api_tokens TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON identity.user_api_tokens TO notrelix_app;
DROP POLICY IF EXISTS identity_user_api_tokens_worker_all ON identity.user_api_tokens;
CREATE POLICY identity_user_api_tokens_worker_all ON identity.user_api_tokens FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS identity_user_api_tokens_support_select ON identity.user_api_tokens;
CREATE POLICY identity_user_api_tokens_support_select ON identity.user_api_tokens FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS identity_user_api_tokens_app_select ON identity.user_api_tokens;
CREATE POLICY identity_user_api_tokens_app_select ON identity.user_api_tokens FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS identity_user_api_tokens_app_insert ON identity.user_api_tokens;
CREATE POLICY identity_user_api_tokens_app_insert ON identity.user_api_tokens FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS identity_user_api_tokens_app_update ON identity.user_api_tokens;
CREATE POLICY identity_user_api_tokens_app_update ON identity.user_api_tokens FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_members ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_members TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_members TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_members TO notrelix_app;
DROP POLICY IF EXISTS account_account_members_worker_all ON account.account_members;
CREATE POLICY account_account_members_worker_all ON account.account_members FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_members_support_select ON account.account_members;
CREATE POLICY account_account_members_support_select ON account.account_members FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_members_app_select ON account.account_members;
CREATE POLICY account_account_members_app_select ON account.account_members FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_members_app_insert ON account.account_members;
CREATE POLICY account_account_members_app_insert ON account.account_members FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_members_app_update ON account.account_members;
CREATE POLICY account_account_members_app_update ON account.account_members FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_invitations ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_invitations TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_invitations TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_invitations TO notrelix_app;
DROP POLICY IF EXISTS account_account_invitations_worker_all ON account.account_invitations;
CREATE POLICY account_account_invitations_worker_all ON account.account_invitations FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_invitations_support_select ON account.account_invitations;
CREATE POLICY account_account_invitations_support_select ON account.account_invitations FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_invitations_app_select ON account.account_invitations;
CREATE POLICY account_account_invitations_app_select ON account.account_invitations FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_invitations_app_insert ON account.account_invitations;
CREATE POLICY account_account_invitations_app_insert ON account.account_invitations FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_invitations_app_update ON account.account_invitations;
CREATE POLICY account_account_invitations_app_update ON account.account_invitations FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_domains ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_domains TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_domains TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_domains TO notrelix_app;
DROP POLICY IF EXISTS account_account_domains_worker_all ON account.account_domains;
CREATE POLICY account_account_domains_worker_all ON account.account_domains FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_domains_support_select ON account.account_domains;
CREATE POLICY account_account_domains_support_select ON account.account_domains FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_domains_app_select ON account.account_domains;
CREATE POLICY account_account_domains_app_select ON account.account_domains FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_domains_app_insert ON account.account_domains;
CREATE POLICY account_account_domains_app_insert ON account.account_domains FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_domains_app_update ON account.account_domains;
CREATE POLICY account_account_domains_app_update ON account.account_domains FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_settings ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_settings TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_settings TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_settings TO notrelix_app;
DROP POLICY IF EXISTS account_account_settings_worker_all ON account.account_settings;
CREATE POLICY account_account_settings_worker_all ON account.account_settings FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_settings_support_select ON account.account_settings;
CREATE POLICY account_account_settings_support_select ON account.account_settings FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_settings_app_select ON account.account_settings;
CREATE POLICY account_account_settings_app_select ON account.account_settings FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_settings_app_insert ON account.account_settings;
CREATE POLICY account_account_settings_app_insert ON account.account_settings FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_settings_app_update ON account.account_settings;
CREATE POLICY account_account_settings_app_update ON account.account_settings FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_regions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_regions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_regions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_regions TO notrelix_app;
DROP POLICY IF EXISTS account_account_regions_worker_all ON account.account_regions;
CREATE POLICY account_account_regions_worker_all ON account.account_regions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_regions_support_select ON account.account_regions;
CREATE POLICY account_account_regions_support_select ON account.account_regions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_regions_app_select ON account.account_regions;
CREATE POLICY account_account_regions_app_select ON account.account_regions FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_regions_app_insert ON account.account_regions;
CREATE POLICY account_account_regions_app_insert ON account.account_regions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_regions_app_update ON account.account_regions;
CREATE POLICY account_account_regions_app_update ON account.account_regions FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.account_identity_providers ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.account_identity_providers TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.account_identity_providers TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.account_identity_providers TO notrelix_app;
DROP POLICY IF EXISTS account_account_identity_providers_worker_all ON account.account_identity_providers;
CREATE POLICY account_account_identity_providers_worker_all ON account.account_identity_providers FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_account_identity_providers_support_select ON account.account_identity_providers;
CREATE POLICY account_account_identity_providers_support_select ON account.account_identity_providers FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_identity_providers_app_select ON account.account_identity_providers;
CREATE POLICY account_account_identity_providers_app_select ON account.account_identity_providers FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_identity_providers_app_insert ON account.account_identity_providers;
CREATE POLICY account_account_identity_providers_app_insert ON account.account_identity_providers FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_account_identity_providers_app_update ON account.account_identity_providers;
CREATE POLICY account_account_identity_providers_app_update ON account.account_identity_providers FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.scim_directories ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.scim_directories TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.scim_directories TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.scim_directories TO notrelix_app;
DROP POLICY IF EXISTS account_scim_directories_worker_all ON account.scim_directories;
CREATE POLICY account_scim_directories_worker_all ON account.scim_directories FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_scim_directories_support_select ON account.scim_directories;
CREATE POLICY account_scim_directories_support_select ON account.scim_directories FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_scim_directories_app_select ON account.scim_directories;
CREATE POLICY account_scim_directories_app_select ON account.scim_directories FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_scim_directories_app_insert ON account.scim_directories;
CREATE POLICY account_scim_directories_app_insert ON account.scim_directories FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_scim_directories_app_update ON account.scim_directories;
CREATE POLICY account_scim_directories_app_update ON account.scim_directories FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE account.scim_sync_runs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.scim_sync_runs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.scim_sync_runs TO notrelix_worker;
GRANT SELECT ON account.scim_sync_runs TO notrelix_app;
DROP POLICY IF EXISTS account_scim_sync_runs_worker_all ON account.scim_sync_runs;
CREATE POLICY account_scim_sync_runs_worker_all ON account.scim_sync_runs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_scim_sync_runs_support_select ON account.scim_sync_runs;
CREATE POLICY account_scim_sync_runs_support_select ON account.scim_sync_runs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_scim_sync_runs_app_select ON account.scim_sync_runs;
CREATE POLICY account_scim_sync_runs_app_select ON account.scim_sync_runs FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));

ALTER TABLE account.workspace_routes ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON account.workspace_routes TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON account.workspace_routes TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON account.workspace_routes TO notrelix_app;
DROP POLICY IF EXISTS account_workspace_routes_worker_all ON account.workspace_routes;
CREATE POLICY account_workspace_routes_worker_all ON account.workspace_routes FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS account_workspace_routes_support_select ON account.workspace_routes;
CREATE POLICY account_workspace_routes_support_select ON account.workspace_routes FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_workspace_routes_app_select ON account.workspace_routes;
CREATE POLICY account_workspace_routes_app_select ON account.workspace_routes FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_workspace_routes_app_insert ON account.workspace_routes;
CREATE POLICY account_workspace_routes_app_insert ON account.workspace_routes FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS account_workspace_routes_app_update ON account.workspace_routes;
CREATE POLICY account_workspace_routes_app_update ON account.workspace_routes FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE workspace.workspaces ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.workspaces TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.workspaces TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.workspaces TO notrelix_app;
DROP POLICY IF EXISTS workspace_workspaces_worker_all ON workspace.workspaces;
CREATE POLICY workspace_workspaces_worker_all ON workspace.workspaces FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_workspaces_support_select ON workspace.workspaces;
CREATE POLICY workspace_workspaces_support_select ON workspace.workspaces FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS workspace_workspaces_app_select ON workspace.workspaces;
CREATE POLICY workspace_workspaces_app_select ON workspace.workspaces FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS workspace_workspaces_app_insert ON workspace.workspaces;
CREATE POLICY workspace_workspaces_app_insert ON workspace.workspaces FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS workspace_workspaces_app_update ON workspace.workspaces;
CREATE POLICY workspace_workspaces_app_update ON workspace.workspaces FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE workspace.workspace_members ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.workspace_members TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.workspace_members TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.workspace_members TO notrelix_app;
DROP POLICY IF EXISTS workspace_workspace_members_worker_all ON workspace.workspace_members;
CREATE POLICY workspace_workspace_members_worker_all ON workspace.workspace_members FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_workspace_members_support_select ON workspace.workspace_members;
CREATE POLICY workspace_workspace_members_support_select ON workspace.workspace_members FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_members_app_select ON workspace.workspace_members;
CREATE POLICY workspace_workspace_members_app_select ON workspace.workspace_members FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_members_app_insert ON workspace.workspace_members;
CREATE POLICY workspace_workspace_members_app_insert ON workspace.workspace_members FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_members_app_update ON workspace.workspace_members;
CREATE POLICY workspace_workspace_members_app_update ON workspace.workspace_members FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE workspace.workspace_invitations ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.workspace_invitations TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.workspace_invitations TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.workspace_invitations TO notrelix_app;
DROP POLICY IF EXISTS workspace_workspace_invitations_worker_all ON workspace.workspace_invitations;
CREATE POLICY workspace_workspace_invitations_worker_all ON workspace.workspace_invitations FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_workspace_invitations_support_select ON workspace.workspace_invitations;
CREATE POLICY workspace_workspace_invitations_support_select ON workspace.workspace_invitations FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_invitations_app_select ON workspace.workspace_invitations;
CREATE POLICY workspace_workspace_invitations_app_select ON workspace.workspace_invitations FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_invitations_app_insert ON workspace.workspace_invitations;
CREATE POLICY workspace_workspace_invitations_app_insert ON workspace.workspace_invitations FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_workspace_invitations_app_update ON workspace.workspace_invitations;
CREATE POLICY workspace_workspace_invitations_app_update ON workspace.workspace_invitations FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE workspace.spaces ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.spaces TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.spaces TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.spaces TO notrelix_app;
DROP POLICY IF EXISTS workspace_spaces_worker_all ON workspace.spaces;
CREATE POLICY workspace_spaces_worker_all ON workspace.spaces FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_spaces_support_select ON workspace.spaces;
CREATE POLICY workspace_spaces_support_select ON workspace.spaces FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_spaces_app_select ON workspace.spaces;
CREATE POLICY workspace_spaces_app_select ON workspace.spaces FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_spaces_app_insert ON workspace.spaces;
CREATE POLICY workspace_spaces_app_insert ON workspace.spaces FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_spaces_app_update ON workspace.spaces;
CREATE POLICY workspace_spaces_app_update ON workspace.spaces FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE workspace.teams ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.teams TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.teams TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.teams TO notrelix_app;
DROP POLICY IF EXISTS workspace_teams_worker_all ON workspace.teams;
CREATE POLICY workspace_teams_worker_all ON workspace.teams FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_teams_support_select ON workspace.teams;
CREATE POLICY workspace_teams_support_select ON workspace.teams FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_teams_app_select ON workspace.teams;
CREATE POLICY workspace_teams_app_select ON workspace.teams FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_teams_app_insert ON workspace.teams;
CREATE POLICY workspace_teams_app_insert ON workspace.teams FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_teams_app_update ON workspace.teams;
CREATE POLICY workspace_teams_app_update ON workspace.teams FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE workspace.team_members ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON workspace.team_members TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspace.team_members TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON workspace.team_members TO notrelix_app;
DROP POLICY IF EXISTS workspace_team_members_worker_all ON workspace.team_members;
CREATE POLICY workspace_team_members_worker_all ON workspace.team_members FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS workspace_team_members_support_select ON workspace.team_members;
CREATE POLICY workspace_team_members_support_select ON workspace.team_members FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_team_members_app_select ON workspace.team_members;
CREATE POLICY workspace_team_members_app_select ON workspace.team_members FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_team_members_app_insert ON workspace.team_members;
CREATE POLICY workspace_team_members_app_insert ON workspace.team_members FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS workspace_team_members_app_update ON workspace.team_members;
CREATE POLICY workspace_team_members_app_update ON workspace.team_members FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.custom_roles ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.custom_roles TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.custom_roles TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.custom_roles TO notrelix_app;
DROP POLICY IF EXISTS governance_custom_roles_worker_all ON governance.custom_roles;
CREATE POLICY governance_custom_roles_worker_all ON governance.custom_roles FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_custom_roles_support_select ON governance.custom_roles;
CREATE POLICY governance_custom_roles_support_select ON governance.custom_roles FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_roles_app_select ON governance.custom_roles;
CREATE POLICY governance_custom_roles_app_select ON governance.custom_roles FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_roles_app_insert ON governance.custom_roles;
CREATE POLICY governance_custom_roles_app_insert ON governance.custom_roles FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_roles_app_update ON governance.custom_roles;
CREATE POLICY governance_custom_roles_app_update ON governance.custom_roles FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.custom_role_permissions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.custom_role_permissions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.custom_role_permissions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.custom_role_permissions TO notrelix_app;
DROP POLICY IF EXISTS governance_custom_role_permissions_worker_all ON governance.custom_role_permissions;
CREATE POLICY governance_custom_role_permissions_worker_all ON governance.custom_role_permissions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_custom_role_permissions_support_select ON governance.custom_role_permissions;
CREATE POLICY governance_custom_role_permissions_support_select ON governance.custom_role_permissions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_role_permissions_app_select ON governance.custom_role_permissions;
CREATE POLICY governance_custom_role_permissions_app_select ON governance.custom_role_permissions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_role_permissions_app_insert ON governance.custom_role_permissions;
CREATE POLICY governance_custom_role_permissions_app_insert ON governance.custom_role_permissions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_custom_role_permissions_app_update ON governance.custom_role_permissions;
CREATE POLICY governance_custom_role_permissions_app_update ON governance.custom_role_permissions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.workspace_member_role_assignments ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.workspace_member_role_assignments TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.workspace_member_role_assignments TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.workspace_member_role_assignments TO notrelix_app;
DROP POLICY IF EXISTS governance_workspace_member_role_assignments_worker_all ON governance.workspace_member_role_assignments;
CREATE POLICY governance_workspace_member_role_assignments_worker_all ON governance.workspace_member_role_assignments FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_workspace_member_role_assignments_support_select ON governance.workspace_member_role_assignments;
CREATE POLICY governance_workspace_member_role_assignments_support_select ON governance.workspace_member_role_assignments FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_member_role_assignments_app_select ON governance.workspace_member_role_assignments;
CREATE POLICY governance_workspace_member_role_assignments_app_select ON governance.workspace_member_role_assignments FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_member_role_assignments_app_insert ON governance.workspace_member_role_assignments;
CREATE POLICY governance_workspace_member_role_assignments_app_insert ON governance.workspace_member_role_assignments FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_member_role_assignments_app_update ON governance.workspace_member_role_assignments;
CREATE POLICY governance_workspace_member_role_assignments_app_update ON governance.workspace_member_role_assignments FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.resource_permissions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.resource_permissions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.resource_permissions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.resource_permissions TO notrelix_app;
DROP POLICY IF EXISTS governance_resource_permissions_worker_all ON governance.resource_permissions;
CREATE POLICY governance_resource_permissions_worker_all ON governance.resource_permissions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_resource_permissions_support_select ON governance.resource_permissions;
CREATE POLICY governance_resource_permissions_support_select ON governance.resource_permissions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_resource_permissions_app_select ON governance.resource_permissions;
CREATE POLICY governance_resource_permissions_app_select ON governance.resource_permissions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_resource_permissions_app_insert ON governance.resource_permissions;
CREATE POLICY governance_resource_permissions_app_insert ON governance.resource_permissions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_resource_permissions_app_update ON governance.resource_permissions;
CREATE POLICY governance_resource_permissions_app_update ON governance.resource_permissions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.field_permissions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.field_permissions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.field_permissions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.field_permissions TO notrelix_app;
DROP POLICY IF EXISTS governance_field_permissions_worker_all ON governance.field_permissions;
CREATE POLICY governance_field_permissions_worker_all ON governance.field_permissions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_field_permissions_support_select ON governance.field_permissions;
CREATE POLICY governance_field_permissions_support_select ON governance.field_permissions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_field_permissions_app_select ON governance.field_permissions;
CREATE POLICY governance_field_permissions_app_select ON governance.field_permissions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_field_permissions_app_insert ON governance.field_permissions;
CREATE POLICY governance_field_permissions_app_insert ON governance.field_permissions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_field_permissions_app_update ON governance.field_permissions;
CREATE POLICY governance_field_permissions_app_update ON governance.field_permissions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.permission_rules ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.permission_rules TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.permission_rules TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.permission_rules TO notrelix_app;
DROP POLICY IF EXISTS governance_permission_rules_worker_all ON governance.permission_rules;
CREATE POLICY governance_permission_rules_worker_all ON governance.permission_rules FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_permission_rules_support_select ON governance.permission_rules;
CREATE POLICY governance_permission_rules_support_select ON governance.permission_rules FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_rules_app_select ON governance.permission_rules;
CREATE POLICY governance_permission_rules_app_select ON governance.permission_rules FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_rules_app_insert ON governance.permission_rules;
CREATE POLICY governance_permission_rules_app_insert ON governance.permission_rules FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_rules_app_update ON governance.permission_rules;
CREATE POLICY governance_permission_rules_app_update ON governance.permission_rules FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.permission_templates ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.permission_templates TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.permission_templates TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.permission_templates TO notrelix_app;
DROP POLICY IF EXISTS governance_permission_templates_worker_all ON governance.permission_templates;
CREATE POLICY governance_permission_templates_worker_all ON governance.permission_templates FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_permission_templates_support_select ON governance.permission_templates;
CREATE POLICY governance_permission_templates_support_select ON governance.permission_templates FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_templates_app_select ON governance.permission_templates;
CREATE POLICY governance_permission_templates_app_select ON governance.permission_templates FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_templates_app_insert ON governance.permission_templates;
CREATE POLICY governance_permission_templates_app_insert ON governance.permission_templates FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_permission_templates_app_update ON governance.permission_templates;
CREATE POLICY governance_permission_templates_app_update ON governance.permission_templates FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.workspace_policies ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.workspace_policies TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.workspace_policies TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.workspace_policies TO notrelix_app;
DROP POLICY IF EXISTS governance_workspace_policies_worker_all ON governance.workspace_policies;
CREATE POLICY governance_workspace_policies_worker_all ON governance.workspace_policies FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_workspace_policies_support_select ON governance.workspace_policies;
CREATE POLICY governance_workspace_policies_support_select ON governance.workspace_policies FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_policies_app_select ON governance.workspace_policies;
CREATE POLICY governance_workspace_policies_app_select ON governance.workspace_policies FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_policies_app_insert ON governance.workspace_policies;
CREATE POLICY governance_workspace_policies_app_insert ON governance.workspace_policies FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_workspace_policies_app_update ON governance.workspace_policies;
CREATE POLICY governance_workspace_policies_app_update ON governance.workspace_policies FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.share_links ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.share_links TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.share_links TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON governance.share_links TO notrelix_app;
DROP POLICY IF EXISTS governance_share_links_worker_all ON governance.share_links;
CREATE POLICY governance_share_links_worker_all ON governance.share_links FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_share_links_support_select ON governance.share_links;
CREATE POLICY governance_share_links_support_select ON governance.share_links FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_share_links_app_select ON governance.share_links;
CREATE POLICY governance_share_links_app_select ON governance.share_links FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_share_links_app_insert ON governance.share_links;
CREATE POLICY governance_share_links_app_insert ON governance.share_links FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_share_links_app_update ON governance.share_links;
CREATE POLICY governance_share_links_app_update ON governance.share_links FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE governance.resource_permission_inheritance_cache ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON governance.resource_permission_inheritance_cache TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON governance.resource_permission_inheritance_cache TO notrelix_worker;
GRANT SELECT ON governance.resource_permission_inheritance_cache TO notrelix_app;
DROP POLICY IF EXISTS governance_resource_permission_inheritance_cache_worker_all ON governance.resource_permission_inheritance_cache;
CREATE POLICY governance_resource_permission_inheritance_cache_worker_all ON governance.resource_permission_inheritance_cache FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS governance_resource_permission_inheritance_cache_support_select ON governance.resource_permission_inheritance_cache;
CREATE POLICY governance_resource_permission_inheritance_cache_support_select ON governance.resource_permission_inheritance_cache FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS governance_resource_permission_inheritance_cache_app_select ON governance.resource_permission_inheritance_cache;
CREATE POLICY governance_resource_permission_inheritance_cache_app_select ON governance.resource_permission_inheritance_cache FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE authz.access_grants ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON authz.access_grants TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON authz.access_grants TO notrelix_worker;
GRANT SELECT ON authz.access_grants TO notrelix_app;
DROP POLICY IF EXISTS authz_access_grants_worker_all ON authz.access_grants;
CREATE POLICY authz_access_grants_worker_all ON authz.access_grants FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS authz_access_grants_support_select ON authz.access_grants;
CREATE POLICY authz_access_grants_support_select ON authz.access_grants FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS authz_access_grants_app_select ON authz.access_grants;
CREATE POLICY authz_access_grants_app_select ON authz.access_grants FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.boards ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.boards TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.boards TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.boards TO notrelix_app;
DROP POLICY IF EXISTS work_boards_worker_all ON work.boards;
CREATE POLICY work_boards_worker_all ON work.boards FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_boards_support_select ON work.boards;
CREATE POLICY work_boards_support_select ON work.boards FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_boards_app_select ON work.boards;
CREATE POLICY work_boards_app_select ON work.boards FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_boards_app_insert ON work.boards;
CREATE POLICY work_boards_app_insert ON work.boards FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_boards_app_update ON work.boards;
CREATE POLICY work_boards_app_update ON work.boards FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_groups ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_groups TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_groups TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_groups TO notrelix_app;
DROP POLICY IF EXISTS work_board_groups_worker_all ON work.board_groups;
CREATE POLICY work_board_groups_worker_all ON work.board_groups FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_groups_support_select ON work.board_groups;
CREATE POLICY work_board_groups_support_select ON work.board_groups FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_groups_app_select ON work.board_groups;
CREATE POLICY work_board_groups_app_select ON work.board_groups FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_groups_app_insert ON work.board_groups;
CREATE POLICY work_board_groups_app_insert ON work.board_groups FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_groups_app_update ON work.board_groups;
CREATE POLICY work_board_groups_app_update ON work.board_groups FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_fields ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_fields TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_fields TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_fields TO notrelix_app;
DROP POLICY IF EXISTS work_board_fields_worker_all ON work.board_fields;
CREATE POLICY work_board_fields_worker_all ON work.board_fields FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_fields_support_select ON work.board_fields;
CREATE POLICY work_board_fields_support_select ON work.board_fields FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_fields_app_select ON work.board_fields;
CREATE POLICY work_board_fields_app_select ON work.board_fields FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_fields_app_insert ON work.board_fields;
CREATE POLICY work_board_fields_app_insert ON work.board_fields FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_fields_app_update ON work.board_fields;
CREATE POLICY work_board_fields_app_update ON work.board_fields FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.field_options ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.field_options TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.field_options TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.field_options TO notrelix_app;
DROP POLICY IF EXISTS work_field_options_worker_all ON work.field_options;
CREATE POLICY work_field_options_worker_all ON work.field_options FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_field_options_support_select ON work.field_options;
CREATE POLICY work_field_options_support_select ON work.field_options FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_field_options_app_select ON work.field_options;
CREATE POLICY work_field_options_app_select ON work.field_options FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_field_options_app_insert ON work.field_options;
CREATE POLICY work_field_options_app_insert ON work.field_options FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_field_options_app_update ON work.field_options;
CREATE POLICY work_field_options_app_update ON work.field_options FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_items ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_items TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_items TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_items TO notrelix_app;
DROP POLICY IF EXISTS work_board_items_worker_all ON work.board_items;
CREATE POLICY work_board_items_worker_all ON work.board_items FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_items_support_select ON work.board_items;
CREATE POLICY work_board_items_support_select ON work.board_items FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_items_app_select ON work.board_items;
CREATE POLICY work_board_items_app_select ON work.board_items FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_items_app_insert ON work.board_items;
CREATE POLICY work_board_items_app_insert ON work.board_items FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_items_app_update ON work.board_items;
CREATE POLICY work_board_items_app_update ON work.board_items FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_item_values ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_item_values TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_item_values TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_item_values TO notrelix_app;
DROP POLICY IF EXISTS work_board_item_values_worker_all ON work.board_item_values;
CREATE POLICY work_board_item_values_worker_all ON work.board_item_values FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_item_values_support_select ON work.board_item_values;
CREATE POLICY work_board_item_values_support_select ON work.board_item_values FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_values_app_select ON work.board_item_values;
CREATE POLICY work_board_item_values_app_select ON work.board_item_values FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_values_app_insert ON work.board_item_values;
CREATE POLICY work_board_item_values_app_insert ON work.board_item_values FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_values_app_update ON work.board_item_values;
CREATE POLICY work_board_item_values_app_update ON work.board_item_values FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_item_members ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_item_members TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_item_members TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_item_members TO notrelix_app;
DROP POLICY IF EXISTS work_board_item_members_worker_all ON work.board_item_members;
CREATE POLICY work_board_item_members_worker_all ON work.board_item_members FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_item_members_support_select ON work.board_item_members;
CREATE POLICY work_board_item_members_support_select ON work.board_item_members FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_members_app_select ON work.board_item_members;
CREATE POLICY work_board_item_members_app_select ON work.board_item_members FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_members_app_insert ON work.board_item_members;
CREATE POLICY work_board_item_members_app_insert ON work.board_item_members FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_members_app_update ON work.board_item_members;
CREATE POLICY work_board_item_members_app_update ON work.board_item_members FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.labels ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.labels TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.labels TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.labels TO notrelix_app;
DROP POLICY IF EXISTS work_labels_worker_all ON work.labels;
CREATE POLICY work_labels_worker_all ON work.labels FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_labels_support_select ON work.labels;
CREATE POLICY work_labels_support_select ON work.labels FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_labels_app_select ON work.labels;
CREATE POLICY work_labels_app_select ON work.labels FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_labels_app_insert ON work.labels;
CREATE POLICY work_labels_app_insert ON work.labels FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_labels_app_update ON work.labels;
CREATE POLICY work_labels_app_update ON work.labels FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_item_labels ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_item_labels TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_item_labels TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_item_labels TO notrelix_app;
DROP POLICY IF EXISTS work_board_item_labels_worker_all ON work.board_item_labels;
CREATE POLICY work_board_item_labels_worker_all ON work.board_item_labels FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_item_labels_support_select ON work.board_item_labels;
CREATE POLICY work_board_item_labels_support_select ON work.board_item_labels FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_labels_app_select ON work.board_item_labels;
CREATE POLICY work_board_item_labels_app_select ON work.board_item_labels FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_labels_app_insert ON work.board_item_labels;
CREATE POLICY work_board_item_labels_app_insert ON work.board_item_labels FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_labels_app_update ON work.board_item_labels;
CREATE POLICY work_board_item_labels_app_update ON work.board_item_labels FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_views ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_views TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_views TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_views TO notrelix_app;
DROP POLICY IF EXISTS work_board_views_worker_all ON work.board_views;
CREATE POLICY work_board_views_worker_all ON work.board_views FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_views_support_select ON work.board_views;
CREATE POLICY work_board_views_support_select ON work.board_views FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_views_app_select ON work.board_views;
CREATE POLICY work_board_views_app_select ON work.board_views FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_views_app_insert ON work.board_views;
CREATE POLICY work_board_views_app_insert ON work.board_views FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_views_app_update ON work.board_views;
CREATE POLICY work_board_views_app_update ON work.board_views FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_view_user_preferences ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_view_user_preferences TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_view_user_preferences TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_view_user_preferences TO notrelix_app;
DROP POLICY IF EXISTS work_board_view_user_preferences_worker_all ON work.board_view_user_preferences;
CREATE POLICY work_board_view_user_preferences_worker_all ON work.board_view_user_preferences FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_view_user_preferences_support_select ON work.board_view_user_preferences;
CREATE POLICY work_board_view_user_preferences_support_select ON work.board_view_user_preferences FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_user_preferences_app_select ON work.board_view_user_preferences;
CREATE POLICY work_board_view_user_preferences_app_select ON work.board_view_user_preferences FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_user_preferences_app_insert ON work.board_view_user_preferences;
CREATE POLICY work_board_view_user_preferences_app_insert ON work.board_view_user_preferences FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_user_preferences_app_update ON work.board_view_user_preferences;
CREATE POLICY work_board_view_user_preferences_app_update ON work.board_view_user_preferences FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.saved_filters ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.saved_filters TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.saved_filters TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.saved_filters TO notrelix_app;
DROP POLICY IF EXISTS work_saved_filters_worker_all ON work.saved_filters;
CREATE POLICY work_saved_filters_worker_all ON work.saved_filters FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_saved_filters_support_select ON work.saved_filters;
CREATE POLICY work_saved_filters_support_select ON work.saved_filters FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_saved_filters_app_select ON work.saved_filters;
CREATE POLICY work_saved_filters_app_select ON work.saved_filters FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_saved_filters_app_insert ON work.saved_filters;
CREATE POLICY work_saved_filters_app_insert ON work.saved_filters FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_saved_filters_app_update ON work.saved_filters;
CREATE POLICY work_saved_filters_app_update ON work.saved_filters FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_view_pins ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_view_pins TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_view_pins TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_view_pins TO notrelix_app;
DROP POLICY IF EXISTS work_board_view_pins_worker_all ON work.board_view_pins;
CREATE POLICY work_board_view_pins_worker_all ON work.board_view_pins FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_view_pins_support_select ON work.board_view_pins;
CREATE POLICY work_board_view_pins_support_select ON work.board_view_pins FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_pins_app_select ON work.board_view_pins;
CREATE POLICY work_board_view_pins_app_select ON work.board_view_pins FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_pins_app_insert ON work.board_view_pins;
CREATE POLICY work_board_view_pins_app_insert ON work.board_view_pins FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_view_pins_app_update ON work.board_view_pins;
CREATE POLICY work_board_view_pins_app_update ON work.board_view_pins FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_item_links ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_item_links TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_item_links TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_item_links TO notrelix_app;
DROP POLICY IF EXISTS work_board_item_links_worker_all ON work.board_item_links;
CREATE POLICY work_board_item_links_worker_all ON work.board_item_links FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_item_links_support_select ON work.board_item_links;
CREATE POLICY work_board_item_links_support_select ON work.board_item_links FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_links_app_select ON work.board_item_links;
CREATE POLICY work_board_item_links_app_select ON work.board_item_links FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_links_app_insert ON work.board_item_links;
CREATE POLICY work_board_item_links_app_insert ON work.board_item_links FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_links_app_update ON work.board_item_links;
CREATE POLICY work_board_item_links_app_update ON work.board_item_links FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.checklists ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.checklists TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.checklists TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.checklists TO notrelix_app;
DROP POLICY IF EXISTS work_checklists_worker_all ON work.checklists;
CREATE POLICY work_checklists_worker_all ON work.checklists FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_checklists_support_select ON work.checklists;
CREATE POLICY work_checklists_support_select ON work.checklists FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklists_app_select ON work.checklists;
CREATE POLICY work_checklists_app_select ON work.checklists FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklists_app_insert ON work.checklists;
CREATE POLICY work_checklists_app_insert ON work.checklists FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklists_app_update ON work.checklists;
CREATE POLICY work_checklists_app_update ON work.checklists FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.checklist_items ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.checklist_items TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.checklist_items TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.checklist_items TO notrelix_app;
DROP POLICY IF EXISTS work_checklist_items_worker_all ON work.checklist_items;
CREATE POLICY work_checklist_items_worker_all ON work.checklist_items FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_checklist_items_support_select ON work.checklist_items;
CREATE POLICY work_checklist_items_support_select ON work.checklist_items FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklist_items_app_select ON work.checklist_items;
CREATE POLICY work_checklist_items_app_select ON work.checklist_items FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklist_items_app_insert ON work.checklist_items;
CREATE POLICY work_checklist_items_app_insert ON work.checklist_items FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_checklist_items_app_update ON work.checklist_items;
CREATE POLICY work_checklist_items_app_update ON work.checklist_items FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.relation_field_configs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.relation_field_configs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.relation_field_configs TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.relation_field_configs TO notrelix_app;
DROP POLICY IF EXISTS work_relation_field_configs_worker_all ON work.relation_field_configs;
CREATE POLICY work_relation_field_configs_worker_all ON work.relation_field_configs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_relation_field_configs_support_select ON work.relation_field_configs;
CREATE POLICY work_relation_field_configs_support_select ON work.relation_field_configs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_relation_field_configs_app_select ON work.relation_field_configs;
CREATE POLICY work_relation_field_configs_app_select ON work.relation_field_configs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_relation_field_configs_app_insert ON work.relation_field_configs;
CREATE POLICY work_relation_field_configs_app_insert ON work.relation_field_configs FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_relation_field_configs_app_update ON work.relation_field_configs;
CREATE POLICY work_relation_field_configs_app_update ON work.relation_field_configs FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_relations ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_relations TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_relations TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_relations TO notrelix_app;
DROP POLICY IF EXISTS work_board_relations_worker_all ON work.board_relations;
CREATE POLICY work_board_relations_worker_all ON work.board_relations FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_relations_support_select ON work.board_relations;
CREATE POLICY work_board_relations_support_select ON work.board_relations FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_relations_app_select ON work.board_relations;
CREATE POLICY work_board_relations_app_select ON work.board_relations FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_relations_app_insert ON work.board_relations;
CREATE POLICY work_board_relations_app_insert ON work.board_relations FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_relations_app_update ON work.board_relations;
CREATE POLICY work_board_relations_app_update ON work.board_relations FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_item_connections ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_item_connections TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_item_connections TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_item_connections TO notrelix_app;
DROP POLICY IF EXISTS work_board_item_connections_worker_all ON work.board_item_connections;
CREATE POLICY work_board_item_connections_worker_all ON work.board_item_connections FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_item_connections_support_select ON work.board_item_connections;
CREATE POLICY work_board_item_connections_support_select ON work.board_item_connections FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_connections_app_select ON work.board_item_connections;
CREATE POLICY work_board_item_connections_app_select ON work.board_item_connections FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_connections_app_insert ON work.board_item_connections;
CREATE POLICY work_board_item_connections_app_insert ON work.board_item_connections FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_item_connections_app_update ON work.board_item_connections;
CREATE POLICY work_board_item_connections_app_update ON work.board_item_connections FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.formula_dependencies ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.formula_dependencies TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.formula_dependencies TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.formula_dependencies TO notrelix_app;
DROP POLICY IF EXISTS work_formula_dependencies_worker_all ON work.formula_dependencies;
CREATE POLICY work_formula_dependencies_worker_all ON work.formula_dependencies FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_formula_dependencies_support_select ON work.formula_dependencies;
CREATE POLICY work_formula_dependencies_support_select ON work.formula_dependencies FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_formula_dependencies_app_select ON work.formula_dependencies;
CREATE POLICY work_formula_dependencies_app_select ON work.formula_dependencies FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_formula_dependencies_app_insert ON work.formula_dependencies;
CREATE POLICY work_formula_dependencies_app_insert ON work.formula_dependencies FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_formula_dependencies_app_update ON work.formula_dependencies;
CREATE POLICY work_formula_dependencies_app_update ON work.formula_dependencies FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.mirror_value_snapshots ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.mirror_value_snapshots TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.mirror_value_snapshots TO notrelix_worker;
GRANT SELECT ON work.mirror_value_snapshots TO notrelix_app;
DROP POLICY IF EXISTS work_mirror_value_snapshots_worker_all ON work.mirror_value_snapshots;
CREATE POLICY work_mirror_value_snapshots_worker_all ON work.mirror_value_snapshots FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_mirror_value_snapshots_support_select ON work.mirror_value_snapshots;
CREATE POLICY work_mirror_value_snapshots_support_select ON work.mirror_value_snapshots FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_mirror_value_snapshots_app_select ON work.mirror_value_snapshots;
CREATE POLICY work_mirror_value_snapshots_app_select ON work.mirror_value_snapshots FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.rollup_snapshots ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.rollup_snapshots TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.rollup_snapshots TO notrelix_worker;
GRANT SELECT ON work.rollup_snapshots TO notrelix_app;
DROP POLICY IF EXISTS work_rollup_snapshots_worker_all ON work.rollup_snapshots;
CREATE POLICY work_rollup_snapshots_worker_all ON work.rollup_snapshots FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_rollup_snapshots_support_select ON work.rollup_snapshots;
CREATE POLICY work_rollup_snapshots_support_select ON work.rollup_snapshots FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_rollup_snapshots_app_select ON work.rollup_snapshots;
CREATE POLICY work_rollup_snapshots_app_select ON work.rollup_snapshots FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.approval_requests ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.approval_requests TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.approval_requests TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.approval_requests TO notrelix_app;
DROP POLICY IF EXISTS work_approval_requests_worker_all ON work.approval_requests;
CREATE POLICY work_approval_requests_worker_all ON work.approval_requests FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_approval_requests_support_select ON work.approval_requests;
CREATE POLICY work_approval_requests_support_select ON work.approval_requests FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_requests_app_select ON work.approval_requests;
CREATE POLICY work_approval_requests_app_select ON work.approval_requests FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_requests_app_insert ON work.approval_requests;
CREATE POLICY work_approval_requests_app_insert ON work.approval_requests FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_requests_app_update ON work.approval_requests;
CREATE POLICY work_approval_requests_app_update ON work.approval_requests FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.approval_steps ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.approval_steps TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.approval_steps TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.approval_steps TO notrelix_app;
DROP POLICY IF EXISTS work_approval_steps_worker_all ON work.approval_steps;
CREATE POLICY work_approval_steps_worker_all ON work.approval_steps FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_approval_steps_support_select ON work.approval_steps;
CREATE POLICY work_approval_steps_support_select ON work.approval_steps FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_steps_app_select ON work.approval_steps;
CREATE POLICY work_approval_steps_app_select ON work.approval_steps FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_steps_app_insert ON work.approval_steps;
CREATE POLICY work_approval_steps_app_insert ON work.approval_steps FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_approval_steps_app_update ON work.approval_steps;
CREATE POLICY work_approval_steps_app_update ON work.approval_steps FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.workload_allocations ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.workload_allocations TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.workload_allocations TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.workload_allocations TO notrelix_app;
DROP POLICY IF EXISTS work_workload_allocations_worker_all ON work.workload_allocations;
CREATE POLICY work_workload_allocations_worker_all ON work.workload_allocations FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_workload_allocations_support_select ON work.workload_allocations;
CREATE POLICY work_workload_allocations_support_select ON work.workload_allocations FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_workload_allocations_app_select ON work.workload_allocations;
CREATE POLICY work_workload_allocations_app_select ON work.workload_allocations FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_workload_allocations_app_insert ON work.workload_allocations;
CREATE POLICY work_workload_allocations_app_insert ON work.workload_allocations FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_workload_allocations_app_update ON work.workload_allocations;
CREATE POLICY work_workload_allocations_app_update ON work.workload_allocations FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_templates ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_templates TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_templates TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_templates TO notrelix_app;
DROP POLICY IF EXISTS work_board_templates_worker_all ON work.board_templates;
CREATE POLICY work_board_templates_worker_all ON work.board_templates FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_templates_support_select ON work.board_templates;
CREATE POLICY work_board_templates_support_select ON work.board_templates FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_templates_app_select ON work.board_templates;
CREATE POLICY work_board_templates_app_select ON work.board_templates FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_templates_app_insert ON work.board_templates;
CREATE POLICY work_board_templates_app_insert ON work.board_templates FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_templates_app_update ON work.board_templates;
CREATE POLICY work_board_templates_app_update ON work.board_templates FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.item_templates ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.item_templates TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.item_templates TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.item_templates TO notrelix_app;
DROP POLICY IF EXISTS work_item_templates_worker_all ON work.item_templates;
CREATE POLICY work_item_templates_worker_all ON work.item_templates FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_item_templates_support_select ON work.item_templates;
CREATE POLICY work_item_templates_support_select ON work.item_templates FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_templates_app_select ON work.item_templates;
CREATE POLICY work_item_templates_app_select ON work.item_templates FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_templates_app_insert ON work.item_templates;
CREATE POLICY work_item_templates_app_insert ON work.item_templates FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_templates_app_update ON work.item_templates;
CREATE POLICY work_item_templates_app_update ON work.item_templates FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.board_subscribers ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.board_subscribers TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.board_subscribers TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.board_subscribers TO notrelix_app;
DROP POLICY IF EXISTS work_board_subscribers_worker_all ON work.board_subscribers;
CREATE POLICY work_board_subscribers_worker_all ON work.board_subscribers FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_board_subscribers_support_select ON work.board_subscribers;
CREATE POLICY work_board_subscribers_support_select ON work.board_subscribers FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_subscribers_app_select ON work.board_subscribers;
CREATE POLICY work_board_subscribers_app_select ON work.board_subscribers FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_subscribers_app_insert ON work.board_subscribers;
CREATE POLICY work_board_subscribers_app_insert ON work.board_subscribers FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_board_subscribers_app_update ON work.board_subscribers;
CREATE POLICY work_board_subscribers_app_update ON work.board_subscribers FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.item_dependencies ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.item_dependencies TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.item_dependencies TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.item_dependencies TO notrelix_app;
DROP POLICY IF EXISTS work_item_dependencies_worker_all ON work.item_dependencies;
CREATE POLICY work_item_dependencies_worker_all ON work.item_dependencies FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_item_dependencies_support_select ON work.item_dependencies;
CREATE POLICY work_item_dependencies_support_select ON work.item_dependencies FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_dependencies_app_select ON work.item_dependencies;
CREATE POLICY work_item_dependencies_app_select ON work.item_dependencies FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_dependencies_app_insert ON work.item_dependencies;
CREATE POLICY work_item_dependencies_app_insert ON work.item_dependencies FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_item_dependencies_app_update ON work.item_dependencies;
CREATE POLICY work_item_dependencies_app_update ON work.item_dependencies FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.time_tracking_entries ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.time_tracking_entries TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.time_tracking_entries TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.time_tracking_entries TO notrelix_app;
DROP POLICY IF EXISTS work_time_tracking_entries_worker_all ON work.time_tracking_entries;
CREATE POLICY work_time_tracking_entries_worker_all ON work.time_tracking_entries FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_time_tracking_entries_support_select ON work.time_tracking_entries;
CREATE POLICY work_time_tracking_entries_support_select ON work.time_tracking_entries FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_time_tracking_entries_app_select ON work.time_tracking_entries;
CREATE POLICY work_time_tracking_entries_app_select ON work.time_tracking_entries FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_time_tracking_entries_app_insert ON work.time_tracking_entries;
CREATE POLICY work_time_tracking_entries_app_insert ON work.time_tracking_entries FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_time_tracking_entries_app_update ON work.time_tracking_entries;
CREATE POLICY work_time_tracking_entries_app_update ON work.time_tracking_entries FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.forms ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.forms TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.forms TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.forms TO notrelix_app;
DROP POLICY IF EXISTS work_forms_worker_all ON work.forms;
CREATE POLICY work_forms_worker_all ON work.forms FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_forms_support_select ON work.forms;
CREATE POLICY work_forms_support_select ON work.forms FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_forms_app_select ON work.forms;
CREATE POLICY work_forms_app_select ON work.forms FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_forms_app_insert ON work.forms;
CREATE POLICY work_forms_app_insert ON work.forms FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_forms_app_update ON work.forms;
CREATE POLICY work_forms_app_update ON work.forms FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.form_questions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.form_questions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.form_questions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.form_questions TO notrelix_app;
DROP POLICY IF EXISTS work_form_questions_worker_all ON work.form_questions;
CREATE POLICY work_form_questions_worker_all ON work.form_questions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_form_questions_support_select ON work.form_questions;
CREATE POLICY work_form_questions_support_select ON work.form_questions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_questions_app_select ON work.form_questions;
CREATE POLICY work_form_questions_app_select ON work.form_questions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_questions_app_insert ON work.form_questions;
CREATE POLICY work_form_questions_app_insert ON work.form_questions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_questions_app_update ON work.form_questions;
CREATE POLICY work_form_questions_app_update ON work.form_questions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE work.form_submissions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON work.form_submissions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON work.form_submissions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON work.form_submissions TO notrelix_app;
DROP POLICY IF EXISTS work_form_submissions_worker_all ON work.form_submissions;
CREATE POLICY work_form_submissions_worker_all ON work.form_submissions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS work_form_submissions_support_select ON work.form_submissions;
CREATE POLICY work_form_submissions_support_select ON work.form_submissions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_submissions_app_select ON work.form_submissions;
CREATE POLICY work_form_submissions_app_select ON work.form_submissions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_submissions_app_insert ON work.form_submissions;
CREATE POLICY work_form_submissions_app_insert ON work.form_submissions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS work_form_submissions_app_update ON work.form_submissions;
CREATE POLICY work_form_submissions_app_update ON work.form_submissions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE docs.pages ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON docs.pages TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON docs.pages TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON docs.pages TO notrelix_app;
DROP POLICY IF EXISTS docs_pages_worker_all ON docs.pages;
CREATE POLICY docs_pages_worker_all ON docs.pages FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS docs_pages_support_select ON docs.pages;
CREATE POLICY docs_pages_support_select ON docs.pages FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_pages_app_select ON docs.pages;
CREATE POLICY docs_pages_app_select ON docs.pages FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_pages_app_insert ON docs.pages;
CREATE POLICY docs_pages_app_insert ON docs.pages FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_pages_app_update ON docs.pages;
CREATE POLICY docs_pages_app_update ON docs.pages FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE docs.blocks ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON docs.blocks TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON docs.blocks TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON docs.blocks TO notrelix_app;
DROP POLICY IF EXISTS docs_blocks_worker_all ON docs.blocks;
CREATE POLICY docs_blocks_worker_all ON docs.blocks FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS docs_blocks_support_select ON docs.blocks;
CREATE POLICY docs_blocks_support_select ON docs.blocks FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_blocks_app_select ON docs.blocks;
CREATE POLICY docs_blocks_app_select ON docs.blocks FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_blocks_app_insert ON docs.blocks;
CREATE POLICY docs_blocks_app_insert ON docs.blocks FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_blocks_app_update ON docs.blocks;
CREATE POLICY docs_blocks_app_update ON docs.blocks FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE docs.document_versions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON docs.document_versions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON docs.document_versions TO notrelix_worker;
GRANT SELECT ON docs.document_versions TO notrelix_app;
DROP POLICY IF EXISTS docs_document_versions_worker_all ON docs.document_versions;
CREATE POLICY docs_document_versions_worker_all ON docs.document_versions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS docs_document_versions_support_select ON docs.document_versions;
CREATE POLICY docs_document_versions_support_select ON docs.document_versions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_document_versions_app_select ON docs.document_versions;
CREATE POLICY docs_document_versions_app_select ON docs.document_versions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE docs.resource_links ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON docs.resource_links TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON docs.resource_links TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON docs.resource_links TO notrelix_app;
DROP POLICY IF EXISTS docs_resource_links_worker_all ON docs.resource_links;
CREATE POLICY docs_resource_links_worker_all ON docs.resource_links FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS docs_resource_links_support_select ON docs.resource_links;
CREATE POLICY docs_resource_links_support_select ON docs.resource_links FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_resource_links_app_select ON docs.resource_links;
CREATE POLICY docs_resource_links_app_select ON docs.resource_links FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_resource_links_app_insert ON docs.resource_links;
CREATE POLICY docs_resource_links_app_insert ON docs.resource_links FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_resource_links_app_update ON docs.resource_links;
CREATE POLICY docs_resource_links_app_update ON docs.resource_links FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE docs.page_templates ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON docs.page_templates TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON docs.page_templates TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON docs.page_templates TO notrelix_app;
DROP POLICY IF EXISTS docs_page_templates_worker_all ON docs.page_templates;
CREATE POLICY docs_page_templates_worker_all ON docs.page_templates FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS docs_page_templates_support_select ON docs.page_templates;
CREATE POLICY docs_page_templates_support_select ON docs.page_templates FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_page_templates_app_select ON docs.page_templates;
CREATE POLICY docs_page_templates_app_select ON docs.page_templates FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_page_templates_app_insert ON docs.page_templates;
CREATE POLICY docs_page_templates_app_insert ON docs.page_templates FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS docs_page_templates_app_update ON docs.page_templates;
CREATE POLICY docs_page_templates_app_update ON docs.page_templates FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.comments ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.comments TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.comments TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.comments TO notrelix_app;
DROP POLICY IF EXISTS collab_comments_worker_all ON collab.comments;
CREATE POLICY collab_comments_worker_all ON collab.comments FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_comments_support_select ON collab.comments;
CREATE POLICY collab_comments_support_select ON collab.comments FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_comments_app_select ON collab.comments;
CREATE POLICY collab_comments_app_select ON collab.comments FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_comments_app_insert ON collab.comments;
CREATE POLICY collab_comments_app_insert ON collab.comments FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_comments_app_update ON collab.comments;
CREATE POLICY collab_comments_app_update ON collab.comments FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.reactions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.reactions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.reactions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.reactions TO notrelix_app;
DROP POLICY IF EXISTS collab_reactions_worker_all ON collab.reactions;
CREATE POLICY collab_reactions_worker_all ON collab.reactions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_reactions_support_select ON collab.reactions;
CREATE POLICY collab_reactions_support_select ON collab.reactions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_reactions_app_select ON collab.reactions;
CREATE POLICY collab_reactions_app_select ON collab.reactions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_reactions_app_insert ON collab.reactions;
CREATE POLICY collab_reactions_app_insert ON collab.reactions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_reactions_app_update ON collab.reactions;
CREATE POLICY collab_reactions_app_update ON collab.reactions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.mentions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.mentions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.mentions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.mentions TO notrelix_app;
DROP POLICY IF EXISTS collab_mentions_worker_all ON collab.mentions;
CREATE POLICY collab_mentions_worker_all ON collab.mentions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_mentions_support_select ON collab.mentions;
CREATE POLICY collab_mentions_support_select ON collab.mentions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_mentions_app_select ON collab.mentions;
CREATE POLICY collab_mentions_app_select ON collab.mentions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_mentions_app_insert ON collab.mentions;
CREATE POLICY collab_mentions_app_insert ON collab.mentions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_mentions_app_update ON collab.mentions;
CREATE POLICY collab_mentions_app_update ON collab.mentions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.attachments ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.attachments TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.attachments TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.attachments TO notrelix_app;
DROP POLICY IF EXISTS collab_attachments_worker_all ON collab.attachments;
CREATE POLICY collab_attachments_worker_all ON collab.attachments FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_attachments_support_select ON collab.attachments;
CREATE POLICY collab_attachments_support_select ON collab.attachments FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_attachments_app_select ON collab.attachments;
CREATE POLICY collab_attachments_app_select ON collab.attachments FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_attachments_app_insert ON collab.attachments;
CREATE POLICY collab_attachments_app_insert ON collab.attachments FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_attachments_app_update ON collab.attachments;
CREATE POLICY collab_attachments_app_update ON collab.attachments FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.resource_watchers ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.resource_watchers TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.resource_watchers TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.resource_watchers TO notrelix_app;
DROP POLICY IF EXISTS collab_resource_watchers_worker_all ON collab.resource_watchers;
CREATE POLICY collab_resource_watchers_worker_all ON collab.resource_watchers FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_resource_watchers_support_select ON collab.resource_watchers;
CREATE POLICY collab_resource_watchers_support_select ON collab.resource_watchers FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_watchers_app_select ON collab.resource_watchers;
CREATE POLICY collab_resource_watchers_app_select ON collab.resource_watchers FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_watchers_app_insert ON collab.resource_watchers;
CREATE POLICY collab_resource_watchers_app_insert ON collab.resource_watchers FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_watchers_app_update ON collab.resource_watchers;
CREATE POLICY collab_resource_watchers_app_update ON collab.resource_watchers FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.presence_sessions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.presence_sessions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.presence_sessions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.presence_sessions TO notrelix_app;
DROP POLICY IF EXISTS collab_presence_sessions_worker_all ON collab.presence_sessions;
CREATE POLICY collab_presence_sessions_worker_all ON collab.presence_sessions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_presence_sessions_support_select ON collab.presence_sessions;
CREATE POLICY collab_presence_sessions_support_select ON collab.presence_sessions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_presence_sessions_app_select ON collab.presence_sessions;
CREATE POLICY collab_presence_sessions_app_select ON collab.presence_sessions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_presence_sessions_app_insert ON collab.presence_sessions;
CREATE POLICY collab_presence_sessions_app_insert ON collab.presence_sessions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_presence_sessions_app_update ON collab.presence_sessions;
CREATE POLICY collab_presence_sessions_app_update ON collab.presence_sessions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE collab.resource_read_states ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON collab.resource_read_states TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON collab.resource_read_states TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON collab.resource_read_states TO notrelix_app;
DROP POLICY IF EXISTS collab_resource_read_states_worker_all ON collab.resource_read_states;
CREATE POLICY collab_resource_read_states_worker_all ON collab.resource_read_states FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS collab_resource_read_states_support_select ON collab.resource_read_states;
CREATE POLICY collab_resource_read_states_support_select ON collab.resource_read_states FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_read_states_app_select ON collab.resource_read_states;
CREATE POLICY collab_resource_read_states_app_select ON collab.resource_read_states FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_read_states_app_insert ON collab.resource_read_states;
CREATE POLICY collab_resource_read_states_app_insert ON collab.resource_read_states FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS collab_resource_read_states_app_update ON collab.resource_read_states;
CREATE POLICY collab_resource_read_states_app_update ON collab.resource_read_states FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.automation_rules ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.automation_rules TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.automation_rules TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON automation.automation_rules TO notrelix_app;
DROP POLICY IF EXISTS automation_automation_rules_worker_all ON automation.automation_rules;
CREATE POLICY automation_automation_rules_worker_all ON automation.automation_rules FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_automation_rules_support_select ON automation.automation_rules;
CREATE POLICY automation_automation_rules_support_select ON automation.automation_rules FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_rules_app_select ON automation.automation_rules;
CREATE POLICY automation_automation_rules_app_select ON automation.automation_rules FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_rules_app_insert ON automation.automation_rules;
CREATE POLICY automation_automation_rules_app_insert ON automation.automation_rules FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_rules_app_update ON automation.automation_rules;
CREATE POLICY automation_automation_rules_app_update ON automation.automation_rules FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.automation_executions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.automation_executions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.automation_executions TO notrelix_worker;
GRANT SELECT ON automation.automation_executions TO notrelix_app;
DROP POLICY IF EXISTS automation_automation_executions_worker_all ON automation.automation_executions;
CREATE POLICY automation_automation_executions_worker_all ON automation.automation_executions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_automation_executions_support_select ON automation.automation_executions;
CREATE POLICY automation_automation_executions_support_select ON automation.automation_executions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_executions_app_select ON automation.automation_executions;
CREATE POLICY automation_automation_executions_app_select ON automation.automation_executions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.scheduled_jobs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.scheduled_jobs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.scheduled_jobs TO notrelix_worker;
GRANT SELECT ON automation.scheduled_jobs TO notrelix_app;
DROP POLICY IF EXISTS automation_scheduled_jobs_worker_all ON automation.scheduled_jobs;
CREATE POLICY automation_scheduled_jobs_worker_all ON automation.scheduled_jobs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_scheduled_jobs_support_select ON automation.scheduled_jobs;
CREATE POLICY automation_scheduled_jobs_support_select ON automation.scheduled_jobs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_scheduled_jobs_app_select ON automation.scheduled_jobs;
CREATE POLICY automation_scheduled_jobs_app_select ON automation.scheduled_jobs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.automation_templates ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.automation_templates TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.automation_templates TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON automation.automation_templates TO notrelix_app;
DROP POLICY IF EXISTS automation_automation_templates_worker_all ON automation.automation_templates;
CREATE POLICY automation_automation_templates_worker_all ON automation.automation_templates FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_automation_templates_support_select ON automation.automation_templates;
CREATE POLICY automation_automation_templates_support_select ON automation.automation_templates FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_templates_app_select ON automation.automation_templates;
CREATE POLICY automation_automation_templates_app_select ON automation.automation_templates FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_templates_app_insert ON automation.automation_templates;
CREATE POLICY automation_automation_templates_app_insert ON automation.automation_templates FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_automation_templates_app_update ON automation.automation_templates;
CREATE POLICY automation_automation_templates_app_update ON automation.automation_templates FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.ai_agents ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.ai_agents TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.ai_agents TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON automation.ai_agents TO notrelix_app;
DROP POLICY IF EXISTS automation_ai_agents_worker_all ON automation.ai_agents;
CREATE POLICY automation_ai_agents_worker_all ON automation.ai_agents FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_ai_agents_support_select ON automation.ai_agents;
CREATE POLICY automation_ai_agents_support_select ON automation.ai_agents FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_ai_agents_app_select ON automation.ai_agents;
CREATE POLICY automation_ai_agents_app_select ON automation.ai_agents FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_ai_agents_app_insert ON automation.ai_agents;
CREATE POLICY automation_ai_agents_app_insert ON automation.ai_agents FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_ai_agents_app_update ON automation.ai_agents;
CREATE POLICY automation_ai_agents_app_update ON automation.ai_agents FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE automation.ai_agent_runs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON automation.ai_agent_runs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON automation.ai_agent_runs TO notrelix_worker;
GRANT SELECT ON automation.ai_agent_runs TO notrelix_app;
DROP POLICY IF EXISTS automation_ai_agent_runs_worker_all ON automation.ai_agent_runs;
CREATE POLICY automation_ai_agent_runs_worker_all ON automation.ai_agent_runs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS automation_ai_agent_runs_support_select ON automation.ai_agent_runs;
CREATE POLICY automation_ai_agent_runs_support_select ON automation.ai_agent_runs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS automation_ai_agent_runs_app_select ON automation.ai_agent_runs;
CREATE POLICY automation_ai_agent_runs_app_select ON automation.ai_agent_runs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.integration_connections ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.integration_connections TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.integration_connections TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON integration.integration_connections TO notrelix_app;
DROP POLICY IF EXISTS integration_integration_connections_worker_all ON integration.integration_connections;
CREATE POLICY integration_integration_connections_worker_all ON integration.integration_connections FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_integration_connections_support_select ON integration.integration_connections;
CREATE POLICY integration_integration_connections_support_select ON integration.integration_connections FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_connections_app_select ON integration.integration_connections;
CREATE POLICY integration_integration_connections_app_select ON integration.integration_connections FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_connections_app_insert ON integration.integration_connections;
CREATE POLICY integration_integration_connections_app_insert ON integration.integration_connections FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_connections_app_update ON integration.integration_connections;
CREATE POLICY integration_integration_connections_app_update ON integration.integration_connections FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.integration_scopes ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.integration_scopes TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.integration_scopes TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON integration.integration_scopes TO notrelix_app;
DROP POLICY IF EXISTS integration_integration_scopes_worker_all ON integration.integration_scopes;
CREATE POLICY integration_integration_scopes_worker_all ON integration.integration_scopes FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_integration_scopes_support_select ON integration.integration_scopes;
CREATE POLICY integration_integration_scopes_support_select ON integration.integration_scopes FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_scopes_app_select ON integration.integration_scopes;
CREATE POLICY integration_integration_scopes_app_select ON integration.integration_scopes FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_scopes_app_insert ON integration.integration_scopes;
CREATE POLICY integration_integration_scopes_app_insert ON integration.integration_scopes FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_scopes_app_update ON integration.integration_scopes;
CREATE POLICY integration_integration_scopes_app_update ON integration.integration_scopes FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.integration_secret_versions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.integration_secret_versions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.integration_secret_versions TO notrelix_worker;
GRANT SELECT ON integration.integration_secret_versions TO notrelix_app;
DROP POLICY IF EXISTS integration_integration_secret_versions_worker_all ON integration.integration_secret_versions;
CREATE POLICY integration_integration_secret_versions_worker_all ON integration.integration_secret_versions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_integration_secret_versions_support_select ON integration.integration_secret_versions;
CREATE POLICY integration_integration_secret_versions_support_select ON integration.integration_secret_versions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_secret_versions_app_select ON integration.integration_secret_versions;
CREATE POLICY integration_integration_secret_versions_app_select ON integration.integration_secret_versions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.webhook_subscriptions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.webhook_subscriptions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.webhook_subscriptions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON integration.webhook_subscriptions TO notrelix_app;
DROP POLICY IF EXISTS integration_webhook_subscriptions_worker_all ON integration.webhook_subscriptions;
CREATE POLICY integration_webhook_subscriptions_worker_all ON integration.webhook_subscriptions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_webhook_subscriptions_support_select ON integration.webhook_subscriptions;
CREATE POLICY integration_webhook_subscriptions_support_select ON integration.webhook_subscriptions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_webhook_subscriptions_app_select ON integration.webhook_subscriptions;
CREATE POLICY integration_webhook_subscriptions_app_select ON integration.webhook_subscriptions FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_webhook_subscriptions_app_insert ON integration.webhook_subscriptions;
CREATE POLICY integration_webhook_subscriptions_app_insert ON integration.webhook_subscriptions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_webhook_subscriptions_app_update ON integration.webhook_subscriptions;
CREATE POLICY integration_webhook_subscriptions_app_update ON integration.webhook_subscriptions FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.webhook_deliveries ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.webhook_deliveries TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.webhook_deliveries TO notrelix_worker;
GRANT SELECT ON integration.webhook_deliveries TO notrelix_app;
DROP POLICY IF EXISTS integration_webhook_deliveries_worker_all ON integration.webhook_deliveries;
CREATE POLICY integration_webhook_deliveries_worker_all ON integration.webhook_deliveries FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_webhook_deliveries_support_select ON integration.webhook_deliveries;
CREATE POLICY integration_webhook_deliveries_support_select ON integration.webhook_deliveries FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_webhook_deliveries_app_select ON integration.webhook_deliveries;
CREATE POLICY integration_webhook_deliveries_app_select ON integration.webhook_deliveries FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.inbound_webhook_events ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.inbound_webhook_events TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.inbound_webhook_events TO notrelix_worker;
GRANT SELECT ON integration.inbound_webhook_events TO notrelix_app;
DROP POLICY IF EXISTS integration_inbound_webhook_events_worker_all ON integration.inbound_webhook_events;
CREATE POLICY integration_inbound_webhook_events_worker_all ON integration.inbound_webhook_events FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_inbound_webhook_events_support_select ON integration.inbound_webhook_events;
CREATE POLICY integration_inbound_webhook_events_support_select ON integration.inbound_webhook_events FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_inbound_webhook_events_app_select ON integration.inbound_webhook_events;
CREATE POLICY integration_inbound_webhook_events_app_select ON integration.inbound_webhook_events FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.calendar_integrations ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.calendar_integrations TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.calendar_integrations TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON integration.calendar_integrations TO notrelix_app;
DROP POLICY IF EXISTS integration_calendar_integrations_worker_all ON integration.calendar_integrations;
CREATE POLICY integration_calendar_integrations_worker_all ON integration.calendar_integrations FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_calendar_integrations_support_select ON integration.calendar_integrations;
CREATE POLICY integration_calendar_integrations_support_select ON integration.calendar_integrations FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_integrations_app_select ON integration.calendar_integrations;
CREATE POLICY integration_calendar_integrations_app_select ON integration.calendar_integrations FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_integrations_app_insert ON integration.calendar_integrations;
CREATE POLICY integration_calendar_integrations_app_insert ON integration.calendar_integrations FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_integrations_app_update ON integration.calendar_integrations;
CREATE POLICY integration_calendar_integrations_app_update ON integration.calendar_integrations FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.calendar_event_links ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.calendar_event_links TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.calendar_event_links TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON integration.calendar_event_links TO notrelix_app;
DROP POLICY IF EXISTS integration_calendar_event_links_worker_all ON integration.calendar_event_links;
CREATE POLICY integration_calendar_event_links_worker_all ON integration.calendar_event_links FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_calendar_event_links_support_select ON integration.calendar_event_links;
CREATE POLICY integration_calendar_event_links_support_select ON integration.calendar_event_links FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_event_links_app_select ON integration.calendar_event_links;
CREATE POLICY integration_calendar_event_links_app_select ON integration.calendar_event_links FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_event_links_app_insert ON integration.calendar_event_links;
CREATE POLICY integration_calendar_event_links_app_insert ON integration.calendar_event_links FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_calendar_event_links_app_update ON integration.calendar_event_links;
CREATE POLICY integration_calendar_event_links_app_update ON integration.calendar_event_links FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE integration.integration_sync_cursors ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON integration.integration_sync_cursors TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON integration.integration_sync_cursors TO notrelix_worker;
GRANT SELECT ON integration.integration_sync_cursors TO notrelix_app;
DROP POLICY IF EXISTS integration_integration_sync_cursors_worker_all ON integration.integration_sync_cursors;
CREATE POLICY integration_integration_sync_cursors_worker_all ON integration.integration_sync_cursors FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS integration_integration_sync_cursors_support_select ON integration.integration_sync_cursors;
CREATE POLICY integration_integration_sync_cursors_support_select ON integration.integration_sync_cursors FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS integration_integration_sync_cursors_app_select ON integration.integration_sync_cursors;
CREATE POLICY integration_integration_sync_cursors_app_select ON integration.integration_sync_cursors FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE billing.billing_customers ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.billing_customers TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.billing_customers TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.billing_customers TO notrelix_app;
DROP POLICY IF EXISTS billing_billing_customers_worker_all ON billing.billing_customers;
CREATE POLICY billing_billing_customers_worker_all ON billing.billing_customers FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_billing_customers_support_select ON billing.billing_customers;
CREATE POLICY billing_billing_customers_support_select ON billing.billing_customers FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_billing_customers_app_select ON billing.billing_customers;
CREATE POLICY billing_billing_customers_app_select ON billing.billing_customers FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_billing_customers_app_insert ON billing.billing_customers;
CREATE POLICY billing_billing_customers_app_insert ON billing.billing_customers FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_billing_customers_app_update ON billing.billing_customers;
CREATE POLICY billing_billing_customers_app_update ON billing.billing_customers FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.plans ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.plans TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.plans TO notrelix_worker;
GRANT SELECT ON billing.plans TO notrelix_app;
DROP POLICY IF EXISTS billing_plans_worker_all ON billing.plans;
CREATE POLICY billing_plans_worker_all ON billing.plans FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_plans_support_select ON billing.plans;
CREATE POLICY billing_plans_support_select ON billing.plans FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS billing_plans_app_select ON billing.plans;
CREATE POLICY billing_plans_app_select ON billing.plans FOR SELECT TO notrelix_app USING (true);

ALTER TABLE billing.plan_prices ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.plan_prices TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.plan_prices TO notrelix_worker;
GRANT SELECT ON billing.plan_prices TO notrelix_app;
DROP POLICY IF EXISTS billing_plan_prices_worker_all ON billing.plan_prices;
CREATE POLICY billing_plan_prices_worker_all ON billing.plan_prices FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_plan_prices_support_select ON billing.plan_prices;
CREATE POLICY billing_plan_prices_support_select ON billing.plan_prices FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS billing_plan_prices_app_select ON billing.plan_prices;
CREATE POLICY billing_plan_prices_app_select ON billing.plan_prices FOR SELECT TO notrelix_app USING (true);

ALTER TABLE billing.plan_limits ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.plan_limits TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.plan_limits TO notrelix_worker;
GRANT SELECT ON billing.plan_limits TO notrelix_app;
DROP POLICY IF EXISTS billing_plan_limits_worker_all ON billing.plan_limits;
CREATE POLICY billing_plan_limits_worker_all ON billing.plan_limits FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_plan_limits_support_select ON billing.plan_limits;
CREATE POLICY billing_plan_limits_support_select ON billing.plan_limits FOR SELECT TO notrelix_support_readonly USING (true);
DROP POLICY IF EXISTS billing_plan_limits_app_select ON billing.plan_limits;
CREATE POLICY billing_plan_limits_app_select ON billing.plan_limits FOR SELECT TO notrelix_app USING (true);

ALTER TABLE billing.subscriptions ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.subscriptions TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.subscriptions TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.subscriptions TO notrelix_app;
DROP POLICY IF EXISTS billing_subscriptions_worker_all ON billing.subscriptions;
CREATE POLICY billing_subscriptions_worker_all ON billing.subscriptions FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_subscriptions_support_select ON billing.subscriptions;
CREATE POLICY billing_subscriptions_support_select ON billing.subscriptions FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscriptions_app_select ON billing.subscriptions;
CREATE POLICY billing_subscriptions_app_select ON billing.subscriptions FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscriptions_app_insert ON billing.subscriptions;
CREATE POLICY billing_subscriptions_app_insert ON billing.subscriptions FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscriptions_app_update ON billing.subscriptions;
CREATE POLICY billing_subscriptions_app_update ON billing.subscriptions FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.subscription_items ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.subscription_items TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.subscription_items TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.subscription_items TO notrelix_app;
DROP POLICY IF EXISTS billing_subscription_items_worker_all ON billing.subscription_items;
CREATE POLICY billing_subscription_items_worker_all ON billing.subscription_items FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_subscription_items_support_select ON billing.subscription_items;
CREATE POLICY billing_subscription_items_support_select ON billing.subscription_items FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscription_items_app_select ON billing.subscription_items;
CREATE POLICY billing_subscription_items_app_select ON billing.subscription_items FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscription_items_app_insert ON billing.subscription_items;
CREATE POLICY billing_subscription_items_app_insert ON billing.subscription_items FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_subscription_items_app_update ON billing.subscription_items;
CREATE POLICY billing_subscription_items_app_update ON billing.subscription_items FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.payment_methods ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.payment_methods TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.payment_methods TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.payment_methods TO notrelix_app;
DROP POLICY IF EXISTS billing_payment_methods_worker_all ON billing.payment_methods;
CREATE POLICY billing_payment_methods_worker_all ON billing.payment_methods FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_payment_methods_support_select ON billing.payment_methods;
CREATE POLICY billing_payment_methods_support_select ON billing.payment_methods FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_payment_methods_app_select ON billing.payment_methods;
CREATE POLICY billing_payment_methods_app_select ON billing.payment_methods FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_payment_methods_app_insert ON billing.payment_methods;
CREATE POLICY billing_payment_methods_app_insert ON billing.payment_methods FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_payment_methods_app_update ON billing.payment_methods;
CREATE POLICY billing_payment_methods_app_update ON billing.payment_methods FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.invoices ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.invoices TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.invoices TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.invoices TO notrelix_app;
DROP POLICY IF EXISTS billing_invoices_worker_all ON billing.invoices;
CREATE POLICY billing_invoices_worker_all ON billing.invoices FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_invoices_support_select ON billing.invoices;
CREATE POLICY billing_invoices_support_select ON billing.invoices FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoices_app_select ON billing.invoices;
CREATE POLICY billing_invoices_app_select ON billing.invoices FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoices_app_insert ON billing.invoices;
CREATE POLICY billing_invoices_app_insert ON billing.invoices FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoices_app_update ON billing.invoices;
CREATE POLICY billing_invoices_app_update ON billing.invoices FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.invoice_line_items ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.invoice_line_items TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.invoice_line_items TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.invoice_line_items TO notrelix_app;
DROP POLICY IF EXISTS billing_invoice_line_items_worker_all ON billing.invoice_line_items;
CREATE POLICY billing_invoice_line_items_worker_all ON billing.invoice_line_items FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_invoice_line_items_support_select ON billing.invoice_line_items;
CREATE POLICY billing_invoice_line_items_support_select ON billing.invoice_line_items FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoice_line_items_app_select ON billing.invoice_line_items;
CREATE POLICY billing_invoice_line_items_app_select ON billing.invoice_line_items FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoice_line_items_app_insert ON billing.invoice_line_items;
CREATE POLICY billing_invoice_line_items_app_insert ON billing.invoice_line_items FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_invoice_line_items_app_update ON billing.invoice_line_items;
CREATE POLICY billing_invoice_line_items_app_update ON billing.invoice_line_items FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.entitlements ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.entitlements TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.entitlements TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.entitlements TO notrelix_app;
DROP POLICY IF EXISTS billing_entitlements_worker_all ON billing.entitlements;
CREATE POLICY billing_entitlements_worker_all ON billing.entitlements FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_entitlements_support_select ON billing.entitlements;
CREATE POLICY billing_entitlements_support_select ON billing.entitlements FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_entitlements_app_select ON billing.entitlements;
CREATE POLICY billing_entitlements_app_select ON billing.entitlements FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_entitlements_app_insert ON billing.entitlements;
CREATE POLICY billing_entitlements_app_insert ON billing.entitlements FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_entitlements_app_update ON billing.entitlements;
CREATE POLICY billing_entitlements_app_update ON billing.entitlements FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.usage_metrics ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.usage_metrics TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.usage_metrics TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON billing.usage_metrics TO notrelix_app;
DROP POLICY IF EXISTS billing_usage_metrics_worker_all ON billing.usage_metrics;
CREATE POLICY billing_usage_metrics_worker_all ON billing.usage_metrics FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_usage_metrics_support_select ON billing.usage_metrics;
CREATE POLICY billing_usage_metrics_support_select ON billing.usage_metrics FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_usage_metrics_app_select ON billing.usage_metrics;
CREATE POLICY billing_usage_metrics_app_select ON billing.usage_metrics FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_usage_metrics_app_insert ON billing.usage_metrics;
CREATE POLICY billing_usage_metrics_app_insert ON billing.usage_metrics FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_usage_metrics_app_update ON billing.usage_metrics;
CREATE POLICY billing_usage_metrics_app_update ON billing.usage_metrics FOR UPDATE TO notrelix_app USING (authz.can_access_account(account_id)) WITH CHECK (authz.can_access_account(account_id));

ALTER TABLE billing.usage_metric_history ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.usage_metric_history TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.usage_metric_history TO notrelix_worker;
GRANT SELECT ON billing.usage_metric_history TO notrelix_app;
DROP POLICY IF EXISTS billing_usage_metric_history_worker_all ON billing.usage_metric_history;
CREATE POLICY billing_usage_metric_history_worker_all ON billing.usage_metric_history FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_usage_metric_history_support_select ON billing.usage_metric_history;
CREATE POLICY billing_usage_metric_history_support_select ON billing.usage_metric_history FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_usage_metric_history_app_select ON billing.usage_metric_history;
CREATE POLICY billing_usage_metric_history_app_select ON billing.usage_metric_history FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));

ALTER TABLE billing.feature_usage_ledger ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.feature_usage_ledger TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.feature_usage_ledger TO notrelix_worker;
GRANT SELECT ON billing.feature_usage_ledger TO notrelix_app;
DROP POLICY IF EXISTS billing_feature_usage_ledger_worker_all ON billing.feature_usage_ledger;
CREATE POLICY billing_feature_usage_ledger_worker_all ON billing.feature_usage_ledger FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_feature_usage_ledger_support_select ON billing.feature_usage_ledger;
CREATE POLICY billing_feature_usage_ledger_support_select ON billing.feature_usage_ledger FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS billing_feature_usage_ledger_app_select ON billing.feature_usage_ledger;
CREATE POLICY billing_feature_usage_ledger_app_select ON billing.feature_usage_ledger FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE billing.billing_events ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON billing.billing_events TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON billing.billing_events TO notrelix_worker;
GRANT SELECT ON billing.billing_events TO notrelix_app;
DROP POLICY IF EXISTS billing_billing_events_worker_all ON billing.billing_events;
CREATE POLICY billing_billing_events_worker_all ON billing.billing_events FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS billing_billing_events_support_select ON billing.billing_events;
CREATE POLICY billing_billing_events_support_select ON billing.billing_events FOR SELECT TO notrelix_support_readonly USING (authz.can_access_account(account_id));
DROP POLICY IF EXISTS billing_billing_events_app_select ON billing.billing_events;
CREATE POLICY billing_billing_events_app_select ON billing.billing_events FOR SELECT TO notrelix_app USING (authz.can_access_account(account_id));

ALTER TABLE reporting.dashboards ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON reporting.dashboards TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON reporting.dashboards TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON reporting.dashboards TO notrelix_app;
DROP POLICY IF EXISTS reporting_dashboards_worker_all ON reporting.dashboards;
CREATE POLICY reporting_dashboards_worker_all ON reporting.dashboards FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS reporting_dashboards_support_select ON reporting.dashboards;
CREATE POLICY reporting_dashboards_support_select ON reporting.dashboards FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboards_app_select ON reporting.dashboards;
CREATE POLICY reporting_dashboards_app_select ON reporting.dashboards FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboards_app_insert ON reporting.dashboards;
CREATE POLICY reporting_dashboards_app_insert ON reporting.dashboards FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboards_app_update ON reporting.dashboards;
CREATE POLICY reporting_dashboards_app_update ON reporting.dashboards FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE reporting.dashboard_widgets ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON reporting.dashboard_widgets TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON reporting.dashboard_widgets TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON reporting.dashboard_widgets TO notrelix_app;
DROP POLICY IF EXISTS reporting_dashboard_widgets_worker_all ON reporting.dashboard_widgets;
CREATE POLICY reporting_dashboard_widgets_worker_all ON reporting.dashboard_widgets FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS reporting_dashboard_widgets_support_select ON reporting.dashboard_widgets;
CREATE POLICY reporting_dashboard_widgets_support_select ON reporting.dashboard_widgets FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_widgets_app_select ON reporting.dashboard_widgets;
CREATE POLICY reporting_dashboard_widgets_app_select ON reporting.dashboard_widgets FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_widgets_app_insert ON reporting.dashboard_widgets;
CREATE POLICY reporting_dashboard_widgets_app_insert ON reporting.dashboard_widgets FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_widgets_app_update ON reporting.dashboard_widgets;
CREATE POLICY reporting_dashboard_widgets_app_update ON reporting.dashboard_widgets FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE reporting.dashboard_sources ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON reporting.dashboard_sources TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON reporting.dashboard_sources TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON reporting.dashboard_sources TO notrelix_app;
DROP POLICY IF EXISTS reporting_dashboard_sources_worker_all ON reporting.dashboard_sources;
CREATE POLICY reporting_dashboard_sources_worker_all ON reporting.dashboard_sources FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS reporting_dashboard_sources_support_select ON reporting.dashboard_sources;
CREATE POLICY reporting_dashboard_sources_support_select ON reporting.dashboard_sources FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_sources_app_select ON reporting.dashboard_sources;
CREATE POLICY reporting_dashboard_sources_app_select ON reporting.dashboard_sources FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_sources_app_insert ON reporting.dashboard_sources;
CREATE POLICY reporting_dashboard_sources_app_insert ON reporting.dashboard_sources FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_dashboard_sources_app_update ON reporting.dashboard_sources;
CREATE POLICY reporting_dashboard_sources_app_update ON reporting.dashboard_sources FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE reporting.reporting_snapshots ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON reporting.reporting_snapshots TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON reporting.reporting_snapshots TO notrelix_worker;
GRANT SELECT ON reporting.reporting_snapshots TO notrelix_app;
DROP POLICY IF EXISTS reporting_reporting_snapshots_worker_all ON reporting.reporting_snapshots;
CREATE POLICY reporting_reporting_snapshots_worker_all ON reporting.reporting_snapshots FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS reporting_reporting_snapshots_support_select ON reporting.reporting_snapshots;
CREATE POLICY reporting_reporting_snapshots_support_select ON reporting.reporting_snapshots FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS reporting_reporting_snapshots_app_select ON reporting.reporting_snapshots;
CREATE POLICY reporting_reporting_snapshots_app_select ON reporting.reporting_snapshots FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE search.search_documents ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON search.search_documents TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON search.search_documents TO notrelix_worker;
GRANT SELECT ON search.search_documents TO notrelix_app;
DROP POLICY IF EXISTS search_search_documents_worker_all ON search.search_documents;
CREATE POLICY search_search_documents_worker_all ON search.search_documents FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS search_search_documents_support_select ON search.search_documents;
CREATE POLICY search_search_documents_support_select ON search.search_documents FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS search_search_documents_app_select ON search.search_documents;
CREATE POLICY search_search_documents_app_select ON search.search_documents FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE search.search_index_jobs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON search.search_index_jobs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON search.search_index_jobs TO notrelix_worker;
GRANT SELECT ON search.search_index_jobs TO notrelix_app;
DROP POLICY IF EXISTS search_search_index_jobs_worker_all ON search.search_index_jobs;
CREATE POLICY search_search_index_jobs_worker_all ON search.search_index_jobs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS search_search_index_jobs_support_select ON search.search_index_jobs;
CREATE POLICY search_search_index_jobs_support_select ON search.search_index_jobs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS search_search_index_jobs_app_select ON search.search_index_jobs;
CREATE POLICY search_search_index_jobs_app_select ON search.search_index_jobs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.notification_items ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.notification_items TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.notification_items TO notrelix_worker;
GRANT SELECT ON notifications.notification_items TO notrelix_app;
DROP POLICY IF EXISTS notifications_notification_items_worker_all ON notifications.notification_items;
CREATE POLICY notifications_notification_items_worker_all ON notifications.notification_items FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_notification_items_support_select ON notifications.notification_items;
CREATE POLICY notifications_notification_items_support_select ON notifications.notification_items FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_items_app_select ON notifications.notification_items;
CREATE POLICY notifications_notification_items_app_select ON notifications.notification_items FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.notification_recipients ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.notification_recipients TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.notification_recipients TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON notifications.notification_recipients TO notrelix_app;
DROP POLICY IF EXISTS notifications_notification_recipients_worker_all ON notifications.notification_recipients;
CREATE POLICY notifications_notification_recipients_worker_all ON notifications.notification_recipients FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_notification_recipients_support_select ON notifications.notification_recipients;
CREATE POLICY notifications_notification_recipients_support_select ON notifications.notification_recipients FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_recipients_app_select ON notifications.notification_recipients;
CREATE POLICY notifications_notification_recipients_app_select ON notifications.notification_recipients FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_recipients_app_insert ON notifications.notification_recipients;
CREATE POLICY notifications_notification_recipients_app_insert ON notifications.notification_recipients FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_recipients_app_update ON notifications.notification_recipients;
CREATE POLICY notifications_notification_recipients_app_update ON notifications.notification_recipients FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.notification_preferences ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.notification_preferences TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.notification_preferences TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON notifications.notification_preferences TO notrelix_app;
DROP POLICY IF EXISTS notifications_notification_preferences_worker_all ON notifications.notification_preferences;
CREATE POLICY notifications_notification_preferences_worker_all ON notifications.notification_preferences FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_notification_preferences_support_select ON notifications.notification_preferences;
CREATE POLICY notifications_notification_preferences_support_select ON notifications.notification_preferences FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_preferences_app_select ON notifications.notification_preferences;
CREATE POLICY notifications_notification_preferences_app_select ON notifications.notification_preferences FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_preferences_app_insert ON notifications.notification_preferences;
CREATE POLICY notifications_notification_preferences_app_insert ON notifications.notification_preferences FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_preferences_app_update ON notifications.notification_preferences;
CREATE POLICY notifications_notification_preferences_app_update ON notifications.notification_preferences FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.notification_deliveries ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.notification_deliveries TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.notification_deliveries TO notrelix_worker;
GRANT SELECT ON notifications.notification_deliveries TO notrelix_app;
DROP POLICY IF EXISTS notifications_notification_deliveries_worker_all ON notifications.notification_deliveries;
CREATE POLICY notifications_notification_deliveries_worker_all ON notifications.notification_deliveries FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_notification_deliveries_support_select ON notifications.notification_deliveries;
CREATE POLICY notifications_notification_deliveries_support_select ON notifications.notification_deliveries FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_deliveries_app_select ON notifications.notification_deliveries;
CREATE POLICY notifications_notification_deliveries_app_select ON notifications.notification_deliveries FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.notification_counters ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.notification_counters TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.notification_counters TO notrelix_worker;
GRANT SELECT ON notifications.notification_counters TO notrelix_app;
DROP POLICY IF EXISTS notifications_notification_counters_worker_all ON notifications.notification_counters;
CREATE POLICY notifications_notification_counters_worker_all ON notifications.notification_counters FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_notification_counters_support_select ON notifications.notification_counters;
CREATE POLICY notifications_notification_counters_support_select ON notifications.notification_counters FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_notification_counters_app_select ON notifications.notification_counters;
CREATE POLICY notifications_notification_counters_app_select ON notifications.notification_counters FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.email_outbox ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.email_outbox TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.email_outbox TO notrelix_worker;
GRANT SELECT ON notifications.email_outbox TO notrelix_app;
DROP POLICY IF EXISTS notifications_email_outbox_worker_all ON notifications.email_outbox;
CREATE POLICY notifications_email_outbox_worker_all ON notifications.email_outbox FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_email_outbox_support_select ON notifications.email_outbox;
CREATE POLICY notifications_email_outbox_support_select ON notifications.email_outbox FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS notifications_email_outbox_app_select ON notifications.email_outbox;
CREATE POLICY notifications_email_outbox_app_select ON notifications.email_outbox FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE notifications.email_delivery_attempts ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON notifications.email_delivery_attempts TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications.email_delivery_attempts TO notrelix_worker;
GRANT SELECT ON notifications.email_delivery_attempts TO notrelix_app;
DROP POLICY IF EXISTS notifications_email_delivery_attempts_worker_all ON notifications.email_delivery_attempts;
CREATE POLICY notifications_email_delivery_attempts_worker_all ON notifications.email_delivery_attempts FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS notifications_email_delivery_attempts_support_select ON notifications.email_delivery_attempts;
CREATE POLICY notifications_email_delivery_attempts_support_select ON notifications.email_delivery_attempts FOR SELECT TO notrelix_support_readonly USING (true);

ALTER TABLE activity.workspace_activity_logs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON activity.workspace_activity_logs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON activity.workspace_activity_logs TO notrelix_worker;
GRANT SELECT ON activity.workspace_activity_logs TO notrelix_app;
DROP POLICY IF EXISTS activity_workspace_activity_logs_worker_all ON activity.workspace_activity_logs;
CREATE POLICY activity_workspace_activity_logs_worker_all ON activity.workspace_activity_logs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS activity_workspace_activity_logs_support_select ON activity.workspace_activity_logs;
CREATE POLICY activity_workspace_activity_logs_support_select ON activity.workspace_activity_logs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS activity_workspace_activity_logs_app_select ON activity.workspace_activity_logs;
CREATE POLICY activity_workspace_activity_logs_app_select ON activity.workspace_activity_logs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE activity.activity_read_states ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON activity.activity_read_states TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON activity.activity_read_states TO notrelix_worker;
GRANT SELECT, INSERT, UPDATE ON activity.activity_read_states TO notrelix_app;
DROP POLICY IF EXISTS activity_activity_read_states_worker_all ON activity.activity_read_states;
CREATE POLICY activity_activity_read_states_worker_all ON activity.activity_read_states FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS activity_activity_read_states_support_select ON activity.activity_read_states;
CREATE POLICY activity_activity_read_states_support_select ON activity.activity_read_states FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS activity_activity_read_states_app_select ON activity.activity_read_states;
CREATE POLICY activity_activity_read_states_app_select ON activity.activity_read_states FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS activity_activity_read_states_app_insert ON activity.activity_read_states;
CREATE POLICY activity_activity_read_states_app_insert ON activity.activity_read_states FOR INSERT TO notrelix_app WITH CHECK (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS activity_activity_read_states_app_update ON activity.activity_read_states;
CREATE POLICY activity_activity_read_states_app_update ON activity.activity_read_states FOR UPDATE TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id)) WITH CHECK (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE analytics.workspace_usage_daily ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON analytics.workspace_usage_daily TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON analytics.workspace_usage_daily TO notrelix_worker;
GRANT SELECT ON analytics.workspace_usage_daily TO notrelix_app;
DROP POLICY IF EXISTS analytics_workspace_usage_daily_worker_all ON analytics.workspace_usage_daily;
CREATE POLICY analytics_workspace_usage_daily_worker_all ON analytics.workspace_usage_daily FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS analytics_workspace_usage_daily_support_select ON analytics.workspace_usage_daily;
CREATE POLICY analytics_workspace_usage_daily_support_select ON analytics.workspace_usage_daily FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS analytics_workspace_usage_daily_app_select ON analytics.workspace_usage_daily;
CREATE POLICY analytics_workspace_usage_daily_app_select ON analytics.workspace_usage_daily FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE analytics.feature_usage_daily ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON analytics.feature_usage_daily TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON analytics.feature_usage_daily TO notrelix_worker;
GRANT SELECT ON analytics.feature_usage_daily TO notrelix_app;
DROP POLICY IF EXISTS analytics_feature_usage_daily_worker_all ON analytics.feature_usage_daily;
CREATE POLICY analytics_feature_usage_daily_worker_all ON analytics.feature_usage_daily FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS analytics_feature_usage_daily_support_select ON analytics.feature_usage_daily;
CREATE POLICY analytics_feature_usage_daily_support_select ON analytics.feature_usage_daily FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS analytics_feature_usage_daily_app_select ON analytics.feature_usage_daily;
CREATE POLICY analytics_feature_usage_daily_app_select ON analytics.feature_usage_daily FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE events.domain_event_logs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON events.domain_event_logs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON events.domain_event_logs TO notrelix_worker;
GRANT SELECT ON events.domain_event_logs TO notrelix_app;
DROP POLICY IF EXISTS events_domain_event_logs_worker_all ON events.domain_event_logs;
CREATE POLICY events_domain_event_logs_worker_all ON events.domain_event_logs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS events_domain_event_logs_support_select ON events.domain_event_logs;
CREATE POLICY events_domain_event_logs_support_select ON events.domain_event_logs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS events_domain_event_logs_app_select ON events.domain_event_logs;
CREATE POLICY events_domain_event_logs_app_select ON events.domain_event_logs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE messaging.outbox_messages ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON messaging.outbox_messages TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON messaging.outbox_messages TO notrelix_worker;
GRANT SELECT ON messaging.outbox_messages TO notrelix_app;
DROP POLICY IF EXISTS messaging_outbox_messages_worker_all ON messaging.outbox_messages;
CREATE POLICY messaging_outbox_messages_worker_all ON messaging.outbox_messages FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS messaging_outbox_messages_support_select ON messaging.outbox_messages;
CREATE POLICY messaging_outbox_messages_support_select ON messaging.outbox_messages FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS messaging_outbox_messages_app_select ON messaging.outbox_messages;
CREATE POLICY messaging_outbox_messages_app_select ON messaging.outbox_messages FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE messaging.outbox_delivery_attempts ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON messaging.outbox_delivery_attempts TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON messaging.outbox_delivery_attempts TO notrelix_worker;
GRANT SELECT ON messaging.outbox_delivery_attempts TO notrelix_app;
DROP POLICY IF EXISTS messaging_outbox_delivery_attempts_worker_all ON messaging.outbox_delivery_attempts;
CREATE POLICY messaging_outbox_delivery_attempts_worker_all ON messaging.outbox_delivery_attempts FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS messaging_outbox_delivery_attempts_support_select ON messaging.outbox_delivery_attempts;
CREATE POLICY messaging_outbox_delivery_attempts_support_select ON messaging.outbox_delivery_attempts FOR SELECT TO notrelix_support_readonly USING (true);

ALTER TABLE messaging.processed_events ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON messaging.processed_events TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON messaging.processed_events TO notrelix_worker;
GRANT SELECT ON messaging.processed_events TO notrelix_app;
DROP POLICY IF EXISTS messaging_processed_events_worker_all ON messaging.processed_events;
CREATE POLICY messaging_processed_events_worker_all ON messaging.processed_events FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS messaging_processed_events_support_select ON messaging.processed_events;
CREATE POLICY messaging_processed_events_support_select ON messaging.processed_events FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS messaging_processed_events_app_select ON messaging.processed_events;
CREATE POLICY messaging_processed_events_app_select ON messaging.processed_events FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE audit.audit_logs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON audit.audit_logs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON audit.audit_logs TO notrelix_worker;
GRANT SELECT ON audit.audit_logs TO notrelix_app;
DROP POLICY IF EXISTS audit_audit_logs_worker_all ON audit.audit_logs;
CREATE POLICY audit_audit_logs_worker_all ON audit.audit_logs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS audit_audit_logs_support_select ON audit.audit_logs;
CREATE POLICY audit_audit_logs_support_select ON audit.audit_logs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS audit_audit_logs_app_select ON audit.audit_logs;
CREATE POLICY audit_audit_logs_app_select ON audit.audit_logs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE audit.security_events ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON audit.security_events TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON audit.security_events TO notrelix_worker;
GRANT SELECT ON audit.security_events TO notrelix_app;
DROP POLICY IF EXISTS audit_security_events_worker_all ON audit.security_events;
CREATE POLICY audit_security_events_worker_all ON audit.security_events FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS audit_security_events_support_select ON audit.security_events;
CREATE POLICY audit_security_events_support_select ON audit.security_events FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS audit_security_events_app_select ON audit.security_events;
CREATE POLICY audit_security_events_app_select ON audit.security_events FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE ops.idempotency_keys ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON ops.idempotency_keys TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ops.idempotency_keys TO notrelix_worker;
GRANT SELECT ON ops.idempotency_keys TO notrelix_app;
DROP POLICY IF EXISTS ops_idempotency_keys_worker_all ON ops.idempotency_keys;
CREATE POLICY ops_idempotency_keys_worker_all ON ops.idempotency_keys FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS ops_idempotency_keys_support_select ON ops.idempotency_keys;
CREATE POLICY ops_idempotency_keys_support_select ON ops.idempotency_keys FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS ops_idempotency_keys_app_select ON ops.idempotency_keys;
CREATE POLICY ops_idempotency_keys_app_select ON ops.idempotency_keys FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE ops.job_locks ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON ops.job_locks TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ops.job_locks TO notrelix_worker;
GRANT SELECT ON ops.job_locks TO notrelix_app;
DROP POLICY IF EXISTS ops_job_locks_worker_all ON ops.job_locks;
CREATE POLICY ops_job_locks_worker_all ON ops.job_locks FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS ops_job_locks_support_select ON ops.job_locks;
CREATE POLICY ops_job_locks_support_select ON ops.job_locks FOR SELECT TO notrelix_support_readonly USING (true);

ALTER TABLE ops.import_jobs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON ops.import_jobs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ops.import_jobs TO notrelix_worker;
GRANT SELECT ON ops.import_jobs TO notrelix_app;
DROP POLICY IF EXISTS ops_import_jobs_worker_all ON ops.import_jobs;
CREATE POLICY ops_import_jobs_worker_all ON ops.import_jobs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS ops_import_jobs_support_select ON ops.import_jobs;
CREATE POLICY ops_import_jobs_support_select ON ops.import_jobs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS ops_import_jobs_app_select ON ops.import_jobs;
CREATE POLICY ops_import_jobs_app_select ON ops.import_jobs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE ops.export_jobs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON ops.export_jobs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ops.export_jobs TO notrelix_worker;
GRANT SELECT ON ops.export_jobs TO notrelix_app;
DROP POLICY IF EXISTS ops_export_jobs_worker_all ON ops.export_jobs;
CREATE POLICY ops_export_jobs_worker_all ON ops.export_jobs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS ops_export_jobs_support_select ON ops.export_jobs;
CREATE POLICY ops_export_jobs_support_select ON ops.export_jobs FOR SELECT TO notrelix_support_readonly USING (authz.can_access_workspace(account_id, workspace_id));
DROP POLICY IF EXISTS ops_export_jobs_app_select ON ops.export_jobs;
CREATE POLICY ops_export_jobs_app_select ON ops.export_jobs FOR SELECT TO notrelix_app USING (authz.can_access_workspace(account_id, workspace_id));

ALTER TABLE ops.cleanup_runs ENABLE ROW LEVEL SECURITY;
GRANT SELECT ON ops.cleanup_runs TO notrelix_support_readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ops.cleanup_runs TO notrelix_worker;
GRANT SELECT ON ops.cleanup_runs TO notrelix_app;
DROP POLICY IF EXISTS ops_cleanup_runs_worker_all ON ops.cleanup_runs;
CREATE POLICY ops_cleanup_runs_worker_all ON ops.cleanup_runs FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS ops_cleanup_runs_support_select ON ops.cleanup_runs;
CREATE POLICY ops_cleanup_runs_support_select ON ops.cleanup_runs FOR SELECT TO notrelix_support_readonly USING (true);

-- SECTION 10: VERIFICATION QUERIES
-- Run these after applying the baseline.
/*
-- 1. Legacy/compatibility tables must not exist:
WITH forbidden(schema_name, table_name) AS (VALUES
        ('collab','notifications'),
        ('collab','notification_preferences'),
        ('collab','notification_deliveries'),
        ('collab','unread_counters'),
        ('collab','activity_logs'),
        ('audit','activity_logs'),
        ('governance','audit_logs'),
        ('governance','security_events'),
        ('automation','outbox_messages'),
        ('ops','processed_events')
)
SELECT f.*
FROM forbidden f
JOIN information_schema.tables t
  ON t.table_schema = f.schema_name AND t.table_name = f.table_name;

-- 2. Table count by schema:
SELECT table_schema, count(*)
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
GROUP BY table_schema
ORDER BY table_schema;

-- 3. RLS-enabled tables without policies should be zero:
SELECT n.nspname AS schema_name, c.relname AS table_name
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'r'
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND c.relrowsecurity
  AND NOT EXISTS (SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid)
ORDER BY 1,2;

-- 4. SECURITY DEFINER functions without explicit search_path should be zero:
SELECT n.nspname, p.proname
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE p.prosecdef
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND NOT EXISTS (SELECT 1 FROM unnest(coalesce(p.proconfig, ARRAY[]::text[])) cfg WHERE cfg LIKE 'search_path=%');
*/

COMMIT;

-- =============================================================================
-- 016_policies_derived_scope.sql — Tables whose scope is not a column of their own
-- =============================================================================
-- Every table below carries no account_id/workspace_id of its own (or names its owner column differently),
-- so the generic business policy of 007/008 cannot derive a scope for it. Each one inherits the scope of the
-- row it belongs to, exactly as the work child tables do in 008; the parent's own policies still apply to the
-- subquery, so a child is visible only when its parent is.
-- =============================================================================

-- Automation: a step belongs to one execution.
SELECT ops.apply_scope_expression_policies('automation', 'automation_execution_steps',
    'EXISTS (SELECT 1 FROM automation.automation_executions e WHERE e.id = automation_execution_steps.execution_id AND ops.has_workspace_access(e.account_id, e.workspace_id))');

-- Automation templates are a shared catalog (no tenant column, no owner): readable by the application role,
-- written only by the background principal.
SELECT ops.apply_catalog_policies('automation', 'automation_templates');

-- Billing: line items, subscription items and usage history belong to an invoice, subscription or metric.
SELECT ops.apply_scope_expression_policies('billing', 'invoice_line_items',
    'EXISTS (SELECT 1 FROM billing.invoices i WHERE i.id = invoice_line_items.invoice_id AND ops.has_workspace_access(i.account_id, i.workspace_id))');
SELECT ops.apply_scope_expression_policies('billing', 'subscription_items',
    'EXISTS (SELECT 1 FROM billing.subscriptions s WHERE s.id = subscription_items.subscription_id AND ops.has_workspace_access(s.account_id, s.workspace_id))');
SELECT ops.apply_scope_expression_policies('billing', 'usage_metric_history',
    'EXISTS (SELECT 1 FROM billing.usage_metrics m WHERE m.id = usage_metric_history.metric_id AND ops.has_workspace_access(m.account_id, m.workspace_id))');

-- Governance: a custom role is Account-scoped (custom_roles carries account_id only) and its permissions belong
-- to the role. An assignment is workspace-scoped, and an Account Owner/Admin also reaches every assignment of
-- their account so archiving a role can move its holders in workspaces they are not a member of.
SELECT ops.apply_scope_expression_policies('governance', 'custom_role_permissions',
    'EXISTS (SELECT 1 FROM governance.custom_roles r WHERE r.id = custom_role_permissions.custom_role_id AND ops.has_account_access(r.account_id))');
SELECT ops.apply_scope_expression_policies('governance', 'member_role_assignments',
    'ops.has_workspace_access(account_id, workspace_id) OR ops.is_account_admin(account_id)');
SELECT ops.apply_scoped_business_policies('governance', 'custom_role_changes', true);

-- Identity: recovery codes are the person's own; a code belongs to a batch.
SELECT ops.apply_user_owned_policies('identity', 'mfa_recovery_batches', 'user_id', true, true);
SELECT ops.apply_scope_expression_policies('identity', 'mfa_recovery_codes',
    'EXISTS (SELECT 1 FROM identity.mfa_recovery_batches b WHERE b.id = mfa_recovery_codes.batch_id AND b.user_id = ops.current_user_id())');

-- Integrations: calendar rows belong to a calendar integration; connection satellites to a connection.
SELECT ops.apply_scope_expression_policies('integration', 'calendar_events',
    'EXISTS (SELECT 1 FROM integration.calendar_integrations c WHERE c.id = calendar_events.integration_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scope_expression_policies('integration', 'calendar_event_links',
    'EXISTS (SELECT 1 FROM integration.calendar_integrations c WHERE c.id = calendar_event_links.integration_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scope_expression_policies('integration', 'integration_scopes',
    'EXISTS (SELECT 1 FROM integration.integration_connections c WHERE c.id = integration_scopes.connection_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scope_expression_policies('integration', 'integration_secret_versions',
    'EXISTS (SELECT 1 FROM integration.integration_connections c WHERE c.id = integration_secret_versions.connection_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scope_expression_policies('integration', 'integration_sync_cursors',
    'EXISTS (SELECT 1 FROM integration.integration_connections c WHERE c.id = integration_sync_cursors.connection_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scoped_business_policies('integration', 'inbound_webhook_receipts', true);

-- Integration secret blobs carry only an encrypted payload; the row id is the opaque reference recorded on
-- the connection's secret version, which stages the blob in the same transaction. A blob is created inside
-- a tenant scope the caller holds, and read or revoked only through a secret version of a connection in a
-- scope the caller holds. The application role never deletes one.
DO $$
BEGIN
    IF ops.table_exists('integration', 'integration_secret_blobs') THEN
        PERFORM ops.drop_all_policies_for_table('integration', 'integration_secret_blobs');
        PERFORM ops.enable_rls_for_table('integration', 'integration_secret_blobs');
        PERFORM ops.create_policy('integration', 'integration_secret_blobs', 'p_app_insert', 'INSERT', 'notrelix_app', NULL,
            'ops.has_workspace_access(ops.current_account_id(), ops.current_workspace_id())');
        PERFORM ops.create_policy('integration', 'integration_secret_blobs', 'p_app_select', 'SELECT', 'notrelix_app',
            'EXISTS (SELECT 1 FROM integration.integration_secret_versions v JOIN integration.integration_connections c ON c.id = v.connection_id WHERE v.secret_reference = integration_secret_blobs.id::text AND ops.has_workspace_access(c.account_id, c.workspace_id))', NULL);
        PERFORM ops.create_policy('integration', 'integration_secret_blobs', 'p_app_update', 'UPDATE', 'notrelix_app',
            'EXISTS (SELECT 1 FROM integration.integration_secret_versions v JOIN integration.integration_connections c ON c.id = v.connection_id WHERE v.secret_reference = integration_secret_blobs.id::text AND ops.has_workspace_access(c.account_id, c.workspace_id))',
            'EXISTS (SELECT 1 FROM integration.integration_secret_versions v JOIN integration.integration_connections c ON c.id = v.connection_id WHERE v.secret_reference = integration_secret_blobs.id::text AND ops.has_workspace_access(c.account_id, c.workspace_id))');
        PERFORM ops.create_policy('integration', 'integration_secret_blobs', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('integration', 'integration_secret_blobs', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

-- Notifications: a recipient row is the recipient's own (the owner column is recipient_user_id).
SELECT ops.apply_user_owned_policies('notifications', 'notification_recipients', 'recipient_user_id', false, true);

-- Idempotency: the partition string is tenant-qualified by construction
-- (IdempotencyPartitionFactory): account:{N}, account:{N}:workspace:{N}, account:{N}:user:{N}, or system.
-- A record is visible and writable only inside a scope the caller holds; "system" is the background
-- principal's alone.
CREATE OR REPLACE FUNCTION ops.has_idempotency_scope(p_scope text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_scope ~ '^account:[0-9a-f]{32}:workspace:[0-9a-f]{32}$'
            THEN ops.has_workspace_access(substr(p_scope, 9, 32)::uuid, substr(p_scope, 52, 32)::uuid)
        WHEN p_scope ~ '^account:[0-9a-f]{32}:user:[0-9a-f]{32}$'
            THEN substr(p_scope, 47, 32)::uuid = ops.current_user_id()
                 AND ops.has_account_access(substr(p_scope, 9, 32)::uuid)
        WHEN p_scope ~ '^account:[0-9a-f]{32}$'
            THEN ops.has_account_access(substr(p_scope, 9, 32)::uuid)
        ELSE false
    END;
$$;
REVOKE ALL ON FUNCTION ops.has_idempotency_scope(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_idempotency_scope(text) TO notrelix_app, notrelix_worker;

SELECT ops.apply_scope_expression_policies('ops', 'idempotency_records', 'ops.has_idempotency_scope(scope)');

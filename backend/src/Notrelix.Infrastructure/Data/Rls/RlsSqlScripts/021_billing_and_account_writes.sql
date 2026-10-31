-- =============================================================================
-- 021_billing_and_account_writes.sql — No tenant-side commercial billing writes, no plan or status writes
-- =============================================================================
-- Removes access only; runs after 005, which re-grants writes on every apply.
-- =============================================================================

-- 3. Commercial billing state is written by the platform and the worker, never by a tenant request. Reads stay.
-- Usage accounting stays writable where a tenant request records consumption (workspace_feature_usages) and
-- appends to the ledger; the ledger itself is never updated or deleted by the application login.
DO $$
DECLARE
    t text;
    p text;
    commercial text[] := ARRAY[
        'billing_customers', 'payment_methods', 'invoices', 'invoice_line_items', 'subscription_items',
        'entitlements', 'billing_events', 'usage_metrics', 'usage_metric_history',
        'plans', 'plan_prices', 'plan_limits'];
BEGIN
    FOREACH t IN ARRAY commercial
    LOOP
        IF ops.table_exists('billing', t) THEN
            EXECUTE format('REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON billing.%I FROM notrelix_app, notrelix_auth', t);
            FOREACH p IN ARRAY ARRAY['p_app_insert', 'p_app_update', 'p_app_delete']
            LOOP
                EXECUTE format('DROP POLICY IF EXISTS %I ON billing.%I', p, t);
            END LOOP;
        END IF;
    END LOOP;

    IF ops.table_exists('billing', 'feature_usage_ledger') THEN
        EXECUTE 'REVOKE UPDATE, DELETE, TRUNCATE ON billing.feature_usage_ledger FROM notrelix_app, notrelix_auth';
        EXECUTE 'DROP POLICY IF EXISTS p_app_update ON billing.feature_usage_ledger';
        EXECUTE 'DROP POLICY IF EXISTS p_app_delete ON billing.feature_usage_ledger';
    END IF;
END $$;

-- 4. A tenant administrator may rename the account; the plan, the lifecycle status and the deletion marker
-- belong to the platform operator. Column grants replace the table-wide UPDATE.
DO $$
BEGIN
    IF ops.table_exists('account', 'accounts') THEN
        EXECUTE 'REVOKE UPDATE ON account.accounts FROM notrelix_app, notrelix_auth';
        EXECUTE 'GRANT UPDATE (name, legal_name, default_region_code, updated_at, updated_by, version) ON account.accounts TO notrelix_app';
    END IF;
END $$;

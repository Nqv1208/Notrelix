-- =============================================================================
-- 002_helpers.sql — RLS session context helper functions
-- =============================================================================
-- These functions read PostgreSQL session settings set via set_config().
-- They are used by RLS policies to evaluate access rules.
-- =============================================================================

-- updated_at trigger function
CREATE OR REPLACE FUNCTION ops.set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- version increment trigger function
CREATE OR REPLACE FUNCTION ops.increment_version()
RETURNS trigger AS $$
BEGIN
    NEW.version = COALESCE(OLD.version, 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- RLS session context helpers
CREATE OR REPLACE FUNCTION ops.current_user_id() RETURNS uuid AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', true), '')::uuid;
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

CREATE OR REPLACE FUNCTION ops.current_request_scope() RETURNS text AS $$
BEGIN
    RETURN NULLIF(current_setting('app.request_scope', true), '');
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

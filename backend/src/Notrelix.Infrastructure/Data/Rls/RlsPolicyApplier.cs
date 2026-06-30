using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsPolicyApplier
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RlsPolicyApplier> _logger;

    public RlsPolicyApplier(ApplicationDbContext context, ILogger<RlsPolicyApplier> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        var sql = """
            DO $$
            DECLARE
                tbl text;
                sch text;
                workspace_schemas text[] := ARRAY[
                    'workspace','governance','work','docs','collab',
                    'automation','integration','billing','reporting',
                    'activity','search','analytics','notifications'
                ];
            BEGIN
                FOREACH sch IN ARRAY workspace_schemas
                LOOP
                    IF NOT EXISTS(SELECT 1 FROM information_schema.schemata s WHERE s.schema_name = sch) THEN
                        CONTINUE;
                    END IF;

                    FOR tbl IN
                        SELECT c.table_name FROM information_schema.columns c
                        WHERE c.table_schema = sch
                          AND c.column_name = 'workspace_id'
                          AND c.table_name NOT IN ('workspace_usage_daily','feature_usage_daily')
                        ORDER BY c.table_name
                    LOOP
                        EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', sch, tbl);
                        EXECUTE format('DROP POLICY IF EXISTS workspace_access ON %I.%I', sch, tbl);
                        EXECUTE format(
                            'CREATE POLICY workspace_access ON %I.%I FOR ALL USING (authz.current_user_has_workspace_access("workspace_id") OR ops.current_request_scope() = ''system'')',
                            sch, tbl
                        );
                    END LOOP;
                END LOOP;

                -- Identity (user-scoped) tables
                FOREACH tbl IN ARRAY ARRAY['users','user_profiles','user_sessions','oauth_accounts',
                    'user_security_settings','user_mfa_methods','email_verification_tokens','password_reset_tokens']
                LOOP
                    IF NOT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = 'identity' AND table_name = tbl) THEN
                        CONTINUE;
                    END IF;
                    EXECUTE format('ALTER TABLE identity.%I ENABLE ROW LEVEL SECURITY', tbl);
                    EXECUTE format('DROP POLICY IF EXISTS user_access ON identity.%I', tbl);
                    IF tbl = 'users' THEN
                        EXECUTE format('CREATE POLICY user_access ON identity.%I FOR ALL USING (id = ops.current_user_id() OR ops.current_request_scope() = ''system'')', tbl);
                    ELSE
                        EXECUTE format('CREATE POLICY user_access ON identity.%I FOR ALL USING (user_id = ops.current_user_id() OR ops.current_request_scope() = ''system'')', tbl);
                    END IF;
                END LOOP;

                -- Reference tables (read-all policy)
                FOREACH tbl IN ARRAY ARRAY['plans','plan_limits']
                LOOP
                    IF NOT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = 'billing' AND table_name = tbl) THEN
                        CONTINUE;
                    END IF;
                    EXECUTE format('ALTER TABLE billing.%I ENABLE ROW LEVEL SECURITY', tbl);
                    EXECUTE format('DROP POLICY IF EXISTS read_all ON billing.%I', tbl);
                    EXECUTE format('CREATE POLICY read_all ON billing.%I FOR SELECT USING (true)', tbl);
                END LOOP;
            END;
            $$;
            """;

        try
        {
            await _context.Database.ExecuteSqlRawAsync(sql, ct);
            _logger.LogInformation("RLS policies applied via PL/pgSQL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply RLS policies");
            throw;
        }
    }
}

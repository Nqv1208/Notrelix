namespace Notrelix.Infrastructure.Data.Identity;

/// <summary>
/// Existing-data step of migration <c>GrantExistingApiTokensEveryScope</c>: a token issued before scopes existed
/// carried none (a null column). From now on a token with no scope reaches nothing, so each such token is granted
/// all seven scopes explicitly, so it keeps working on the work, docs and chat requests and the six
/// <c>workspace:read</c> queries. It is NOT unchanged: scope enforcement also refuses every token the contexts no
/// scope maps (Accounts, Analytics, Automation, Billing, Collaboration, Governance, Identity, Integrations,
/// Notifications, Operators, and Workspaces except those six queries), so an existing token loses access to them
/// whatever scopes it holds, and a token only ever narrows from here on. The seven scopes are written out here, not
/// read from the live vocabulary: migration history is append-only, and a scope added later must not widen a token
/// retroactively.
/// <para>
/// Only rows whose scopes are still null are written, so a re-run changes nothing; a row that already holds a list
/// (even an empty one, meaning no access) is never touched. The table is not forced for its owner, so the migration
/// (run as the owner or a <c>BYPASSRLS</c> role) sees every tenant's rows; under any other identity row-level
/// security would hide rows, so it first fails with <see cref="BlockedPrefix"/>, and finally fails with
/// <see cref="IncompletePrefix"/> if any token is still without scopes.
/// </para>
/// </summary>
public static class ApiTokenScopeMigrationSql
{
    public const string BlockedPrefix = "API_TOKEN_SCOPE_BACKFILL_BLOCKED";
    public const string IncompletePrefix = "API_TOKEN_SCOPE_BACKFILL_INCOMPLETE";

    public const string EveryScopeJson =
        """["work:read","work:write","docs:read","docs:write","chat:read","chat:write","workspace:read"]""";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('identity.api_tokens') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the identity table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        UPDATE identity.api_tokens
           SET scopes = '{{EveryScopeJson}}'
         WHERE scopes IS NULL;

        DO $$
        DECLARE
            v_missing bigint;
        BEGIN
            SELECT count(*) INTO v_missing FROM identity.api_tokens WHERE scopes IS NULL;
            IF v_missing > 0 THEN
                RAISE EXCEPTION '{{IncompletePrefix}}: % API token(s) still have no scopes', v_missing;
            END IF;
        END $$;
        """;
}

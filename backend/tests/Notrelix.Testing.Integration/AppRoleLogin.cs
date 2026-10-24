using Npgsql;

namespace Notrelix.Testing.Integration;

/// <summary>
/// Creates a non-superuser login that runs as <c>notrelix_app</c>, the role the serving process is meant
/// to use: not the table owner, no BYPASSRLS, so FORCE ROW LEVEL SECURITY applies to it. The roles come
/// from the policy pack, so it must already be applied.
/// </summary>
public static class AppRoleLogin
{
    public const string ApplicationRole = "notrelix_app";

    /// <summary>
    /// Creates the login (idempotently by name) as the owner and returns a connection string for it.
    /// The password is generated per call and never leaves the process.
    /// </summary>
    public static async Task<string> CreateAsync(string ownerConnectionString, string loginName)
    {
        if (loginName.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
        {
            throw new ArgumentException("Login name must be an unquoted identifier.", nameof(loginName));
        }

        var secret = Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE ROLE {loginName} LOGIN PASSWORD '{secret}' IN ROLE {ApplicationRole};";
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(ownerConnectionString)
        {
            Username = loginName,
            Password = secret,
            Options = $"-c role={ApplicationRole}",
        }.ConnectionString;
    }
}

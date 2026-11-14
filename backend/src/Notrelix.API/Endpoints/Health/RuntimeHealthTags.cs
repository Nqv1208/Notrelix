using Notrelix.Infrastructure.Options;

namespace Notrelix.API.Endpoints.Health;

/// <summary>
/// Tags that tie a health check to the process roles that need it. A readiness probe runs the checks tagged
/// <c>ready</c> whose role tag matches a role this process runs.
/// </summary>
public static class RuntimeHealthTags
{
    public const string Ready = "ready";

    public static string For(RuntimeRole role) => "role:" + role.ToString().ToLowerInvariant();

    public static IEnumerable<string> ForRoles(RuntimeRoles roles) =>
        Enum.GetValues<RuntimeRole>().Where(roles.Has).Select(For);
}

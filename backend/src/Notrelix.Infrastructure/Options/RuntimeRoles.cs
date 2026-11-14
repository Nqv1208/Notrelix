namespace Notrelix.Infrastructure.Options;

/// <summary>
/// One process role. The same image runs every role; <c>Runtime:Roles</c> selects which
/// planes this process serves.
/// </summary>
[Flags]
public enum RuntimeRole
{
    /// <summary>REST endpoints (every route except the realtime gateway).</summary>
    Api = 1,

    /// <summary>WebSocket realtime gateway.</summary>
    Realtime = 2,

    /// <summary>Hosted background services: outbox relay, consumers, retention, exports, jobs.</summary>
    Worker = 4,
}

/// <summary>
/// Parsed <c>Runtime:Roles</c> selection. Accepts a comma-separated scalar (<c>Runtime__Roles=api,realtime</c>)
/// or a list (<c>Runtime__Roles__0=api</c>). An absent key means every role, so a single process behaves
/// exactly as before roles existed. Unknown or empty selections fail at startup.
/// </summary>
public sealed class RuntimeRoles
{
    public const string ConfigurationKey = "Runtime:Roles";

    private static readonly IReadOnlyDictionary<string, RuntimeRole> KnownRoles =
        new Dictionary<string, RuntimeRole>(StringComparer.OrdinalIgnoreCase)
        {
            ["api"] = RuntimeRole.Api,
            ["realtime"] = RuntimeRole.Realtime,
            ["worker"] = RuntimeRole.Worker,
        };

    public static RuntimeRoles All { get; } = new(RuntimeRole.Api | RuntimeRole.Realtime | RuntimeRole.Worker);

    public RuntimeRoles(RuntimeRole roles)
    {
        if (roles == 0 || (roles & ~(RuntimeRole.Api | RuntimeRole.Realtime | RuntimeRole.Worker)) != 0)
        {
            throw new InvalidOperationException("Runtime:Roles must select at least one known role.");
        }

        Roles = roles;
    }

    public RuntimeRole Roles { get; }

    public bool RunsApi => Has(RuntimeRole.Api);

    public bool RunsRealtime => Has(RuntimeRole.Realtime);

    public bool RunsWorker => Has(RuntimeRole.Worker);

    public bool Has(RuntimeRole role) => (Roles & role) == role;

    /// <summary>Names of the selected roles in declaration order, for logs and health output.</summary>
    public IReadOnlyList<string> Names =>
        Enum.GetValues<RuntimeRole>().Where(Has).Select(role => role.ToString().ToLowerInvariant()).ToArray();

    public override string ToString() => string.Join(",", Names);

    /// <summary>Reads <c>Runtime:Roles</c>. Throws <see cref="InvalidOperationException"/> on an unknown or empty selection.</summary>
    public static RuntimeRoles Parse(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(ConfigurationKey);
        if (!section.Exists())
        {
            return All;
        }

        IEnumerable<string?> tokens = section.Value is { } scalar
            ? scalar.Split(',')
            : section.GetChildren().Select(child => child.Value);

        return FromTokens(tokens);
    }

    /// <summary>Parses role names; each name is trimmed and compared case-insensitively.</summary>
    public static RuntimeRoles FromTokens(IEnumerable<string?> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var selected = default(RuntimeRole);
        var count = 0;
        foreach (var token in tokens)
        {
            var name = token?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(
                    "Runtime:Roles contains an empty role name. Valid roles: api, realtime, worker.");
            }

            if (!KnownRoles.TryGetValue(name, out var role))
            {
                throw new InvalidOperationException(
                    $"Runtime:Roles contains unknown role '{name}'. Valid roles: api, realtime, worker.");
            }

            selected |= role;
            count++;
        }

        if (count == 0)
        {
            throw new InvalidOperationException(
                "Runtime:Roles must name at least one role (api, realtime, worker) or be omitted to run all roles.");
        }

        return new RuntimeRoles(selected);
    }
}

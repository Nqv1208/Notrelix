namespace Notrelix.Application.Features.Integrations.Connections;

/// <summary>
/// The sync health a connection shows apart from its status (UC-INT-I02; integrations.md §42 and §43). A credential can be
/// valid while sync fails, so health is a separate field and never a value of the status. The set is closed.
/// </summary>
public static class IntegrationSyncHealth
{
    public const string Idle = "idle";
    public const string Syncing = "syncing";
    public const string Degraded = "degraded";
    public const string RateLimited = "rate-limited";
    public const string Failed = "failed";
    public const string ReauthorizationRequired = "reauthorization-required";
    public const string ResyncRequired = "resync-required";

    public static readonly IReadOnlyList<string> All =
    [
        Idle, Syncing, Degraded, RateLimited, Failed, ReauthorizationRequired, ResyncRequired,
    ];

    /// <summary>
    /// The health the stored state supports today: a connection that needs a new grant needs reauthorization, one whose
    /// last delivery failed has failed health, and otherwise it is idle. Syncing, degraded, rate-limited and resync-required
    /// have no source yet and are never produced here.
    /// </summary>
    public static string Of(IntegrationConnectionStatus status) => status switch
    {
        IntegrationConnectionStatus.Expired => ReauthorizationRequired,
        IntegrationConnectionStatus.Error => Failed,
        _ => Idle,
    };
}

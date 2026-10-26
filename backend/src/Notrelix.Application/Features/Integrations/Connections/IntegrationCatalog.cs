namespace Notrelix.Application.Features.Integrations.Connections;

/// <summary>How a provider of the catalog is connected.</summary>
public enum IntegrationConnectMethod
{
    /// <summary>The provider's authorization page, through the backend redirect flow.</summary>
    OAuth,

    /// <summary>An address the backend delivers to, signed with a secret the backend generates.</summary>
    Webhook,
}

/// <summary>One provider the workspace can connect.</summary>
public sealed record IntegrationCatalogEntry(string Key, IntegrationProvider Provider, IntegrationConnectMethod Method);

/// <summary>The providers a workspace can connect: Google Calendar, Slack and an outgoing webhook.</summary>
public static class IntegrationCatalog
{
    public const string GoogleCalendar = "google-calendar";
    public const string Slack = "slack";
    public const string Webhook = "webhook";

    public static IReadOnlyList<IntegrationCatalogEntry> All { get; } =
    [
        new(GoogleCalendar, IntegrationProvider.Google, IntegrationConnectMethod.OAuth),
        new(Slack, IntegrationProvider.Slack, IntegrationConnectMethod.OAuth),
        new(Webhook, IntegrationProvider.Webhook, IntegrationConnectMethod.Webhook),
    ];

    public static IntegrationCatalogEntry? Find(string? key)
        => All.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));

    public static string KeyOf(IntegrationProvider provider)
        => All.FirstOrDefault(entry => entry.Provider == provider)?.Key ?? provider.ToString().ToLowerInvariant();
}

public sealed record IntegrationCatalogEntryDto(string Provider, bool Configured);

public sealed record IntegrationConnectionDto(
    Guid Id,
    string Provider,
    string Status,
    Guid ConnectedBy,
    DateTimeOffset ConnectedAt,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastErrorAt,
    string? LastError,
    string? TargetUrl);

public sealed record IntegrationsOverviewDto(
    IReadOnlyList<IntegrationCatalogEntryDto> Catalog,
    IReadOnlyList<IntegrationConnectionDto> Connections);

public sealed record CreatedWebhookConnectionDto(IntegrationConnectionDto Connection, string SigningSecret);

public sealed record IntegrationAuthorizationDto(string AuthorizeUrl);

public sealed record IntegrationTestDto(Guid DeliveryId);

public static class IntegrationConnectionMapper
{
    public static IntegrationConnectionDto ToDto(IntegrationConnection connection) => new(
        connection.Id,
        IntegrationCatalog.KeyOf(connection.Provider),
        connection.Status switch
        {
            IntegrationConnectionStatus.Active => "active",
            IntegrationConnectionStatus.Paused => "paused",
            _ => "error",
        },
        connection.CreatedBy ?? Guid.Empty,
        connection.CreatedAt,
        connection.LastSuccessAt,
        connection.LastErrorAt,
        connection.ErrorDetail,
        connection.TargetUrl);
}

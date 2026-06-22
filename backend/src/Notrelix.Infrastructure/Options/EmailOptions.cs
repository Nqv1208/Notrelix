namespace Notrelix.Infrastructure.Options;

public sealed class EmailOptions
{
    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = "noreply@notrelix.io";
    public string FromName { get; init; } = "Notrelix";
}

namespace Notrelix.Infrastructure.Email;

public class EmailSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = "noreply@notrelix.io";
    public string FromName { get; init; } = "Notrelix";
}

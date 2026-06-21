namespace Notrelix.Application.Common.Models;

public class N8nOptions
{
    public string InternalBaseUrl { get; set; } = "http://n8n:5678";
    public string WebhookBasePath { get; set; } = "/webhook";
    public string WebhookSecret { get; set; } = string.Empty;
    public int SignatureToleranceSeconds { get; set; } = 300;
}

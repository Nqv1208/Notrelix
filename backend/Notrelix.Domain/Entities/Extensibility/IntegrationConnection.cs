using System.Text.Json;
using Notrelix.Domain.Common;
using Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Domain.Entities.Extensibility;

public class IntegrationConnection : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Settings { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;

    public Workspace Workspace { get; private set; } = null!;

    private IntegrationConnection() { }

    public static IntegrationConnection Create(
        Guid workspaceId,
        Guid createdByUserId,
        string provider,
        string name,
        string settings = "{}")
    {
        ValidateJson(settings);

        return new IntegrationConnection
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdByUserId,
            Provider = Normalize(provider, "n8n"),
            Name = string.IsNullOrWhiteSpace(name) ? "Integration" : name.Trim(),
            Settings = string.IsNullOrWhiteSpace(settings) ? "{}" : settings,
            CreatedBy = createdByUserId
        };
    }

    public void Rename(string name, Guid updatedBy)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
            UpdatedBy = updatedBy;
        }
    }

    public void UpdateSettings(string settings, Guid updatedBy)
    {
        ValidateJson(settings);
        Settings = string.IsNullOrWhiteSpace(settings) ? "{}" : settings;
        UpdatedBy = updatedBy;
    }

    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
    }

    private static string Normalize(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();

    private static void ValidateJson(string value)
    {
        try
        {
            JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Settings must be valid JSON.", nameof(value), ex);
        }
    }
}

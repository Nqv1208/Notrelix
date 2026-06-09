using System.Text.Json;
using Notrelix.Domain.Common;
using Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Domain.Entities.Extensibility;

public class AutomationRule : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? IntegrationConnectionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TriggerEvent { get; private set; } = string.Empty;
    public string ActionType { get; private set; } = string.Empty;
    public string Configuration { get; private set; } = "{}";
    public bool IsEnabled { get; private set; } = true;

    public Workspace Workspace { get; private set; } = null!;
    public IntegrationConnection? IntegrationConnection { get; private set; }

    private AutomationRule() { }

    public static AutomationRule Create(
        Guid workspaceId,
        Guid createdByUserId,
        string name,
        string triggerEvent,
        string actionType,
        string configuration = "{}",
        Guid? integrationConnectionId = null)
    {
        ValidateJson(configuration);

        return new AutomationRule
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdByUserId,
            IntegrationConnectionId = integrationConnectionId,
            Name = string.IsNullOrWhiteSpace(name) ? "Automation" : name.Trim(),
            TriggerEvent = Normalize(triggerEvent),
            ActionType = Normalize(actionType),
            Configuration = string.IsNullOrWhiteSpace(configuration) ? "{}" : configuration,
            CreatedBy = createdByUserId
        };
    }

    public void Update(string name, string triggerEvent, string actionType, string configuration, Guid updatedBy)
    {
        ValidateJson(configuration);

        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        TriggerEvent = string.IsNullOrWhiteSpace(triggerEvent) ? TriggerEvent : Normalize(triggerEvent);
        ActionType = string.IsNullOrWhiteSpace(actionType) ? ActionType : Normalize(actionType);
        Configuration = string.IsNullOrWhiteSpace(configuration) ? "{}" : configuration;
        UpdatedBy = updatedBy;
    }

    public void Enable(Guid updatedBy)
    {
        IsEnabled = true;
        UpdatedBy = updatedBy;
    }

    public void Disable(Guid updatedBy)
    {
        IsEnabled = false;
        UpdatedBy = updatedBy;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static void ValidateJson(string value)
    {
        try
        {
            JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Configuration must be valid JSON.", nameof(value), ex);
        }
    }
}

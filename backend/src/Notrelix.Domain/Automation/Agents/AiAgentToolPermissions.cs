using System.Text.Json;

namespace Notrelix.Domain.Automation.Agents;

public sealed class AiAgentToolPermissions : ValueObject
{
    public bool CanReadBoards { get; }
    public bool CanUpdateItems { get; }
    public bool CanCreateItems { get; }
    public bool CanSendNotifications { get; }
    public bool CanAccessDocuments { get; }
    public bool CanExecuteAutomations { get; }
    public IReadOnlyList<string> AllowedWorkflows { get; }

    private AiAgentToolPermissions()
    {
        AllowedWorkflows = Array.Empty<string>();
    }

    private AiAgentToolPermissions(
        bool canReadBoards,
        bool canUpdateItems,
        bool canCreateItems,
        bool canSendNotifications,
        bool canAccessDocuments,
        bool canExecuteAutomations,
        IReadOnlyList<string> allowedWorkflows)
    {
        CanReadBoards = canReadBoards;
        CanUpdateItems = canUpdateItems;
        CanCreateItems = canCreateItems;
        CanSendNotifications = canSendNotifications;
        CanAccessDocuments = canAccessDocuments;
        CanExecuteAutomations = canExecuteAutomations;
        AllowedWorkflows = allowedWorkflows;
    }

    public static AiAgentToolPermissions Create(
        bool canReadBoards = true,
        bool canUpdateItems = false,
        bool canCreateItems = false,
        bool canSendNotifications = false,
        bool canAccessDocuments = false,
        bool canExecuteAutomations = false,
        IReadOnlyList<string>? allowedWorkflows = null)
    {
        return new AiAgentToolPermissions(
            canReadBoards,
            canUpdateItems,
            canCreateItems,
            canSendNotifications,
            canAccessDocuments,
            canExecuteAutomations,
            allowedWorkflows?.Select(w => w.Trim()).ToList() ?? new List<string>());
    }

    public static AiAgentToolPermissions FromJson(string json)
    {
        Guard.NotNullOrWhiteSpace(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var canReadBoards = root.TryGetProperty("canReadBoards", out var readEl) && readEl.ValueKind == JsonValueKind.True;
            var canUpdateItems = root.TryGetProperty("canUpdateItems", out var updateEl) && updateEl.ValueKind == JsonValueKind.True;
            var canCreateItems = root.TryGetProperty("canCreateItems", out var createEl) && createEl.ValueKind == JsonValueKind.True;
            var canSendNotifications = root.TryGetProperty("canSendNotifications", out var notifEl) && notifEl.ValueKind == JsonValueKind.True;
            var canAccessDocuments = root.TryGetProperty("canAccessDocuments", out var docEl) && docEl.ValueKind == JsonValueKind.True;
            var canExecuteAutomations = root.TryGetProperty("canExecuteAutomations", out var autoEl) && autoEl.ValueKind == JsonValueKind.True;

            var allowedWorkflows = new List<string>();
            if (root.TryGetProperty("allowedWorkflows", out var workflowsEl) && workflowsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in workflowsEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var w = item.GetString();
                        if (w is not null) allowedWorkflows.Add(w);
                    }
                }
            }

            return Create(
                canReadBoards,
                canUpdateItems,
                canCreateItems,
                canSendNotifications,
                canAccessDocuments,
                canExecuteAutomations,
                allowedWorkflows);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid AiAgentToolPermissions JSON: {ex.Message}");
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            canReadBoards = CanReadBoards,
            canUpdateItems = CanUpdateItems,
            canCreateItems = CanCreateItems,
            canSendNotifications = CanSendNotifications,
            canAccessDocuments = CanAccessDocuments,
            canExecuteAutomations = CanExecuteAutomations,
            allowedWorkflows = AllowedWorkflows.Any() ? AllowedWorkflows : null
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CanReadBoards;
        yield return CanUpdateItems;
        yield return CanCreateItems;
        yield return CanSendNotifications;
        yield return CanAccessDocuments;
        yield return CanExecuteAutomations;
        foreach (var workflow in AllowedWorkflows)
        {
            yield return workflow;
        }
    }

    public override string ToString() => ToJson();
}

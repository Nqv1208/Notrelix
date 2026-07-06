using System.Text.Json;

namespace Notrelix.Domain.Analytics.Dashboards;

public static class WidgetConfigValidator
{
    public static (bool IsValid, string? Error) Validate(DashboardWidgetType type, JsonValue config)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(config.Value);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON config: {ex.Message}");
        }

        return type switch
        {
            DashboardWidgetType.BoardWidget => ValidateBoardWidget(root),
            DashboardWidgetType.BoardFieldWidget => ValidateBoardFieldWidget(root),
            DashboardWidgetType.DocumentWidget => ValidateDocumentWidget(root),
            DashboardWidgetType.ReportWidget => ValidateReportWidget(root),
            DashboardWidgetType.TextWidget => ValidateTextWidget(root),
            DashboardWidgetType.ChartWidget => ValidateChartWidget(root),
            _ => (false, $"Unknown widget type '{type}'.")
        };
    }

    private static (bool IsValid, string? Error) ValidateBoardWidget(JsonElement root)
    {
        if (!root.TryGetProperty("boardId", out var boardIdEl) || boardIdEl.ValueKind != JsonValueKind.String)
            return (false, "BoardWidget requires a 'boardId' property.");

        if (!Guid.TryParse(boardIdEl.GetString(), out _))
            return (false, "'boardId' must be a valid GUID.");

        return (true, null);
    }

    private static (bool IsValid, string? Error) ValidateBoardFieldWidget(JsonElement root)
    {
        if (!root.TryGetProperty("boardId", out var boardIdEl) || boardIdEl.ValueKind != JsonValueKind.String)
            return (false, "BoardFieldWidget requires a 'boardId' property.");

        if (!Guid.TryParse(boardIdEl.GetString(), out _))
            return (false, "'boardId' must be a valid GUID.");

        if (!root.TryGetProperty("fieldId", out var fieldIdEl) || fieldIdEl.ValueKind != JsonValueKind.String)
            return (false, "BoardFieldWidget requires a 'fieldId' property.");

        if (!Guid.TryParse(fieldIdEl.GetString(), out _))
            return (false, "'fieldId' must be a valid GUID.");

        return (true, null);
    }

    private static (bool IsValid, string? Error) ValidateDocumentWidget(JsonElement root)
    {
        if (!root.TryGetProperty("pageId", out var pageIdEl) || pageIdEl.ValueKind != JsonValueKind.String)
            return (false, "DocumentWidget requires a 'pageId' property.");

        if (!Guid.TryParse(pageIdEl.GetString(), out _))
            return (false, "'pageId' must be a valid GUID.");

        return (true, null);
    }

    private static (bool IsValid, string? Error) ValidateReportWidget(JsonElement root)
    {
        if (!root.TryGetProperty("reportId", out var reportIdEl) || reportIdEl.ValueKind != JsonValueKind.String)
            return (false, "ReportWidget requires a 'reportId' property.");

        if (!Guid.TryParse(reportIdEl.GetString(), out _))
            return (false, "'reportId' must be a valid GUID.");

        return (true, null);
    }

    private static (bool IsValid, string? Error) ValidateTextWidget(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String)
            return (false, "TextWidget requires a 'content' property.");

        var content = contentEl.GetString();
        if (string.IsNullOrWhiteSpace(content))
            return (false, "'content' must not be empty.");

        return (true, null);
    }

    private static (bool IsValid, string? Error) ValidateChartWidget(JsonElement root)
    {
        if (!root.TryGetProperty("chartType", out var chartTypeEl) || chartTypeEl.ValueKind != JsonValueKind.String)
            return (false, "ChartWidget requires a 'chartType' property.");

        var allowedChartTypes = new[] { "bar", "line", "pie", "area", "donut" };
        var chartType = chartTypeEl.GetString();
        if (chartType is null || !allowedChartTypes.Contains(chartType))
            return (false, $"ChartWidget 'chartType' must be one of: {string.Join(", ", allowedChartTypes)}.");

        if (!root.TryGetProperty("dataSourceId", out var dsIdEl) || dsIdEl.ValueKind != JsonValueKind.String)
            return (false, "ChartWidget requires a 'dataSourceId' property.");

        if (!Guid.TryParse(dsIdEl.GetString(), out _))
            return (false, "'dataSourceId' must be a valid GUID.");

        return (true, null);
    }
}

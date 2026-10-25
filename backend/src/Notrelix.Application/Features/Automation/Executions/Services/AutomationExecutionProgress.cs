using System.Text.Json;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Executions.Services;

/// <summary>
/// What an execution has done so far, one entry per action in the order they run. Kept on the
/// execution so a redelivery resumes after the actions that already succeeded and the history
/// can say what each action did.
/// </summary>
public static class AutomationExecutionProgress
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";

    private sealed record Entry(int Index, string Type, string Status, string? Error);

    public static string Write(IEnumerable<AutomationExecutionStepDto> steps) =>
        JsonSerializer.Serialize(steps.Select(s => new Entry(s.Index, s.Type, s.Status, s.Error)));

    public static IReadOnlyList<AutomationExecutionStepDto>? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<Entry>>(json)?
                .Select(e => new AutomationExecutionStepDto(e.Index, e.Type, e.Status, e.Error))
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

using System.Text.Json;

namespace Notrelix.Domain.Automation.Agents;

public sealed class AiAgentInstruction : ValueObject
{
    public string SystemPrompt { get; }
    public string? AgentDescription { get; }
    public IReadOnlyList<string> Guidelines { get; }
    public string? ToneVoice { get; }

    private AiAgentInstruction()
    {
        SystemPrompt = null!;
        Guidelines = Array.Empty<string>();
    }

    private AiAgentInstruction(string systemPrompt, string? agentDescription, IReadOnlyList<string> guidelines, string? toneVoice)
    {
        SystemPrompt = systemPrompt;
        AgentDescription = agentDescription;
        Guidelines = guidelines;
        ToneVoice = toneVoice;
    }

    public static AiAgentInstruction Create(string systemPrompt, string? agentDescription = null, IReadOnlyList<string>? guidelines = null, string? toneVoice = null)
    {
        Guard.NotNullOrWhiteSpace(systemPrompt);

        return new AiAgentInstruction(
            systemPrompt.Trim(),
            agentDescription?.Trim(),
            guidelines?.Select(g => g.Trim()).ToList() ?? new List<string>(),
            toneVoice?.Trim());
    }

    public static AiAgentInstruction FromJson(string json)
    {
        Guard.NotNullOrWhiteSpace(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var systemPrompt = root.GetProperty("systemPrompt").GetString()!;

            string? agentDescription = null;
            if (root.TryGetProperty("agentDescription", out var descEl) && descEl.ValueKind == JsonValueKind.String)
            {
                agentDescription = descEl.GetString();
            }

            var guidelines = new List<string>();
            if (root.TryGetProperty("guidelines", out var guidelinesEl) && guidelinesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in guidelinesEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var g = item.GetString();
                        if (g is not null) guidelines.Add(g);
                    }
                }
            }

            string? toneVoice = null;
            if (root.TryGetProperty("toneVoice", out var toneEl) && toneEl.ValueKind == JsonValueKind.String)
            {
                toneVoice = toneEl.GetString();
            }

            return Create(systemPrompt, agentDescription, guidelines, toneVoice);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Agent_InvalidInstructionJson, $"Invalid AiAgentInstruction JSON: {ex.Message}");
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            systemPrompt = SystemPrompt,
            agentDescription = AgentDescription,
            guidelines = Guidelines,
            toneVoice = ToneVoice
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SystemPrompt;
        yield return AgentDescription;
        foreach (var guideline in Guidelines)
        {
            yield return guideline;
        }
        yield return ToneVoice;
    }

    public override string ToString() => ToJson();
}

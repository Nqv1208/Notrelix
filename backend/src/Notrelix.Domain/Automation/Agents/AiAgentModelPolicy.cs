using System.Text.Json;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents;

public sealed class AiAgentModelPolicy : ValueObject
{
    public string ModelId { get; }
    public int? MaxTokens { get; }
    public double? Temperature { get; }
    public string? Provider { get; }

    private AiAgentModelPolicy()
    {
        ModelId = null!;
    }

    private AiAgentModelPolicy(string modelId, int? maxTokens, double? temperature, string? provider)
    {
        ModelId = modelId;
        MaxTokens = maxTokens;
        Temperature = temperature;
        Provider = provider;
    }

    public static AiAgentModelPolicy Create(string modelId, int? maxTokens = null, double? temperature = null, string? provider = null)
    {
        Guard.NotNullOrWhiteSpace(modelId);

        if (temperature.HasValue)
        {
            Guard.InRange(temperature.Value, 0.0, 2.0);
        }

        return new AiAgentModelPolicy(modelId.Trim(), maxTokens, temperature, provider?.Trim());
    }

    public static AiAgentModelPolicy FromJson(string json)
    {
        Guard.NotNullOrWhiteSpace(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var modelId = root.GetProperty("modelId").GetString()!;

            int? maxTokens = null;
            if (root.TryGetProperty("maxTokens", out var maxTokensEl) && maxTokensEl.ValueKind == JsonValueKind.Number)
            {
                maxTokens = maxTokensEl.GetInt32();
            }

            double? temperature = null;
            if (root.TryGetProperty("temperature", out var tempEl) && tempEl.ValueKind == JsonValueKind.Number)
            {
                temperature = tempEl.GetDouble();
            }

            string? provider = null;
            if (root.TryGetProperty("provider", out var providerEl) && providerEl.ValueKind == JsonValueKind.String)
            {
                provider = providerEl.GetString();
            }

            return Create(modelId, maxTokens, temperature, provider);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid AiAgentModelPolicy JSON: {ex.Message}");
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            modelId = ModelId,
            maxTokens = MaxTokens,
            temperature = Temperature,
            provider = Provider
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModelId;
        yield return MaxTokens;
        yield return Temperature;
        yield return Provider;
    }

    public override string ToString() => ToJson();
}

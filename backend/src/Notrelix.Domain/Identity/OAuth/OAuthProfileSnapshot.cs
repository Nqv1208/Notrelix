using System.Text.Json;

namespace Notrelix.Domain.Identity.OAuth;

public sealed class OAuthProfileSnapshot : ValueObject
{
    public OAuthProvider Provider { get; }
    public int SchemaVersion { get; }
    public JsonValue Data { get; }

    private OAuthProfileSnapshot(OAuthProvider provider, int schemaVersion, JsonValue data)
    {
        Provider = provider;
        SchemaVersion = schemaVersion;
        Data = data;
    }

    public static OAuthProfileSnapshot Create(
        OAuthProvider provider,
        int schemaVersion,
        JsonValue data)
    {
        Guard.NotNull(data);

        if (schemaVersion <= 0)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_OAuthProfileSnapshot_SchemaVersionMustBePositive,
                "OAuth profile schema version must be positive.");

        try
        {
            using var doc = JsonDocument.Parse(data.Value);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new BusinessRuleException(
                    IdentityRuleCodes.Identity_OAuthProfileSnapshot_DataMustBeJsonObject,
                    "OAuth profile data must be a JSON object.");
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_OAuthProfileSnapshot_DataMustBeJsonObject,
                "OAuth profile data must be valid JSON.");
        }

        return new OAuthProfileSnapshot(provider, schemaVersion, data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Provider;
        yield return SchemaVersion;
        yield return Data;
    }
}

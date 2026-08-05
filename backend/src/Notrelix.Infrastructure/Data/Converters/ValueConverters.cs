using System.Text.Json;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Infrastructure.Data.Converters;

public class ResourceKindConverter : ValueConverter<ResourceKind, string>
{
    public ResourceKindConverter()
        : base(v => v.Value, v => ResourceKind.Create(v))
    {
    }
}

public class JsonValueConverter : ValueConverter<JsonValue, string>
{
    public JsonValueConverter()
        : base(v => v.Value, v => JsonValue.Create(v))
    {
    }
}

public class FractionalIndexConverter : ValueConverter<FractionalIndex, string>
{
    public FractionalIndexConverter()
        : base(v => v.Value, v => FractionalIndex.Create(v))
    {
    }
}

public class SecretRefConverter : ValueConverter<SecretRef, string>
{
    public SecretRefConverter()
        : base(v => v.Value, v => SecretRef.Create(v))
    {
    }
}

public class TokenHashConverter : ValueConverter<TokenHash, string>
{
    public TokenHashConverter()
        : base(v => v.Value, v => TokenHash.Create(v))
    {
    }
}

public class DocumentSnapshotConverter : ValueConverter<DocumentSnapshot, string>
{
    public DocumentSnapshotConverter()
        : base(
            v => v.Data!.Value,
            v => DocumentSnapshot.Create(JsonValue.Create(v)))
    {
    }
}

public class UsageMetricKeyConverter : ValueConverter<UsageMetricKey, string>
{
    public UsageMetricKeyConverter()
        : base(v => v.Value, v => UsageMetricKey.Create(v))
    {
    }
}

public class GroupRuleConverter : ValueConverter<GroupRule, Guid>
{
    public GroupRuleConverter()
        : base(v => v.FieldId, v => GroupRule.Create(v))
    {
    }
}

public class SyncCursorValueConverter : ValueConverter<SyncCursorValue, string>
{
    public SyncCursorValueConverter()
        : base(v => v.Value, v => SyncCursorValue.Create(v))
    {
    }
}

public class ApiTokenScopesConverter : ValueConverter<ApiTokenScopes, string>
{
    public ApiTokenScopesConverter()
        : base(v => v.ToJson(), v => ApiTokenScopes.FromJson(v))
    {
    }
}

public class JsonDocumentConverter : ValueConverter<JsonDocument, string>
{
    public JsonDocumentConverter()
        : base(
            d => d.RootElement.GetRawText(),
            s => JsonDocument.Parse(s, default))
    {
    }
}



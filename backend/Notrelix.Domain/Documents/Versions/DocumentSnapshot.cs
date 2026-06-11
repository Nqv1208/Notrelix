using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Versions;

public sealed class DocumentSnapshot : ValueObject
{
    public JsonValue Data { get; }

    private DocumentSnapshot(JsonValue data)
    {
        Data = data;
    }

    public static DocumentSnapshot Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new DocumentSnapshot(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}

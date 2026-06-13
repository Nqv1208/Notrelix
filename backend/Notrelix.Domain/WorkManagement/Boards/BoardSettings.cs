using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed class BoardSettings : ValueObject
{
    public bool AllowPublicSharing { get; }
    public string? CustomDomain { get; }

    private BoardSettings() { }    private BoardSettings(bool allowPublicSharing, string? customDomain)
    {
        AllowPublicSharing = allowPublicSharing;
        CustomDomain = customDomain;
    }

    public static BoardSettings Create(bool allowPublicSharing = false, string? customDomain = null)
    {
        return new BoardSettings(allowPublicSharing, customDomain);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
        yield return CustomDomain;
    }
}

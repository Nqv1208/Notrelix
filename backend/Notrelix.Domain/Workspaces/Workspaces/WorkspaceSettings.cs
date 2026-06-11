using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed class WorkspaceSettings : ValueObject
{
    public bool AllowPublicSharing { get; }
    public bool EnforceMfa { get; }

    private WorkspaceSettings(bool allowPublicSharing, bool enforceMfa)
    {
        AllowPublicSharing = allowPublicSharing;
        EnforceMfa = enforceMfa;
    }

    public static WorkspaceSettings Create(bool allowPublicSharing = false, bool enforceMfa = false)
    {
        return new WorkspaceSettings(allowPublicSharing, enforceMfa);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
        yield return EnforceMfa;
    }
}

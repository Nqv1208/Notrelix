using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.Freeze;

internal sealed record NonMutationMethod(
    Type AggregateType,
    string MethodSignature,
    string Reason);

internal static class NonMutationMethodRegistry
{
    private static readonly HashSet<(Type, string)> NonMutations = new()
    {
        // Query/predicate methods that inspect state without mutating
        CreateEntry(typeof(Entitlement), "IsActiveAt(System.DateTimeOffset)"),
        CreateEntry(typeof(BoardGroup), "ValidateNotDefaultGroup(System.Guid?)"),
        CreateEntry(typeof(BoardField), "CanBeUsedAsKanbanColumn()"),
        CreateEntry(typeof(Form), "EnsureAcceptsSubmissions()"),
        CreateEntry(typeof(ShareLink), "IsExpired(System.DateTimeOffset)"),
        CreateEntry(typeof(PermissionRule), "IsActive(System.DateTimeOffset)"),
    };

    private static (Type, string) CreateEntry(Type type, string signature)
    {
        return (type, signature);
    }

    public static bool IsNonMutation(Type aggregateType, string methodSignature)
    {
        return NonMutations.Contains((aggregateType, methodSignature));
    }
}

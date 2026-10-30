namespace Notrelix.Application.Features.Governance.ShareLinks;

/// <summary>
/// How a share-link request names the shared resource and which action authorizes it. Create and
/// disable use the same rule, so whoever may make a link on a resource may also turn it off.
/// </summary>
internal static class ShareLinkResources
{
    public static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));

    public static PermissionAction ActionFor(ResourceKind kind) => kind.Value switch
    {
        "work-management.board" => PermissionAction.ShareBoardView,
        "documents.page" => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}

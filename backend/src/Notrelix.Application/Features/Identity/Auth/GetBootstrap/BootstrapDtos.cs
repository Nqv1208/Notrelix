namespace Notrelix.Application.Features.Identity.Auth.GetBootstrap;

public record BootstrapResult
{
    public required UserDto User { get; init; }
    public required List<WorkspaceInfo> Workspaces { get; init; }
    public required PersonalWorkspaceStatus PersonalWorkspace { get; init; }

    /// <summary>
    /// The Account the member acts in: the Account of the default Workspace, else the Account the
    /// member owns, else any Account the member belongs to. Null while the member belongs to none.
    /// </summary>
    public Guid? AccountId { get; init; }
}

public record WorkspaceInfo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Role { get; init; }
    public required Guid AccountId { get; init; }
}

public record PersonalWorkspaceStatus
{
    public required string Status { get; init; }
    public Guid? WorkspaceId { get; init; }
}

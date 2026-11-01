namespace Notrelix.API.Contracts.Governance.Roles.Requests;

public record CreateCustomRoleRequest(
    string Name,
    string? Description,
    string BaseRole,
    IReadOnlyList<string>? AddedActions = null,
    IReadOnlyList<string>? RemovedActions = null);

public record UpdateCustomRoleRequest(
    long ExpectedVersion,
    string Name,
    string? Description,
    IReadOnlyList<string>? AddedActions = null,
    IReadOnlyList<string>? RemovedActions = null);

public record ArchiveCustomRoleRequest(long ExpectedVersion, Guid? MoveToRoleId = null);

public record AssignCustomRoleRequest(Guid RoleId);

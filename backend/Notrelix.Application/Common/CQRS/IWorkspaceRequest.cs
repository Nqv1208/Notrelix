namespace Notrelix.Application.Common.CQRS;

public interface IWorkspaceRequest
{
    Guid WorkspaceId { get; }
}

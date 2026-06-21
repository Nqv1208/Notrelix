namespace Notrelix.Domain.Common;

public interface IWorkspaceScoped
{
    Guid WorkspaceId { get; }
}

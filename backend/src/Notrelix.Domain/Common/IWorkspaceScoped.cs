namespace Notrelix.Domain.Common;

public interface IWorkspaceScoped : IAccountScoped
{
    Guid WorkspaceId { get; }
}

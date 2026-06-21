namespace Notrelix.Application.Common.Abstractions;

public interface ICurrentWorkspace
{
    Guid? WorkspaceId { get; }
    bool IsSet { get; }
    void SetWorkspace(Guid workspaceId);
}

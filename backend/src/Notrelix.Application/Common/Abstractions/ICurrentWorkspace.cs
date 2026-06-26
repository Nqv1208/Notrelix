namespace Notrelix.Application.Common.Abstractions;

public interface ICurrentWorkspace
{
    Guid? WorkspaceId { get; }
    bool IsSet { get; }
    bool IsSystemContext { get; }
    bool HasWorkspace => WorkspaceId.HasValue;
    void SetWorkspace(Guid workspaceId);
    IDisposable EnterSystemContext();
}

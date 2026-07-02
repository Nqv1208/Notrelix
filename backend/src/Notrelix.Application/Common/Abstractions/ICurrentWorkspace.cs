namespace Notrelix.Application.Common.Abstractions;

public interface ICurrentWorkspace
{
    Guid AccountId { get; }
    Guid WorkspaceId { get; }
    bool IsSet { get; }
    bool IsSystemContext { get; }
    void SetWorkspace(Guid accountId, Guid workspaceId);
    IDisposable EnterSystemContext();
}

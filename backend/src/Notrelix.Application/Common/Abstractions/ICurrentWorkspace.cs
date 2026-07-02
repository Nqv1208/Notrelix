namespace Notrelix.Application.Common.Abstractions;

public interface ICurrentWorkspace
{
    Guid? AccountId { get; }
    Guid? WorkspaceId { get; }
    bool IsSet { get; }
    bool IsSystemContext { get; }
    bool HasWorkspace => WorkspaceId.HasValue;
    bool HasAccount => AccountId.HasValue;
    void SetWorkspace(Guid accountId, Guid workspaceId);
    IDisposable EnterSystemContext();
}

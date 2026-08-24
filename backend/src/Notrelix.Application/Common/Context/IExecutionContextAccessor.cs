namespace Notrelix.Application.Common.Context;

public interface IExecutionContextAccessor : IExecutionContextReader
{
    void SetSnapshot(ExecutionContextSnapshot snapshot);
    void SetUser(Guid userId, string email, string name);
    void SetTenant(Guid accountId, Guid workspaceId);
    void SetAccount(Guid accountId);
    void SetCorrelation(Guid correlationId, Guid? causationId = null);
    void SetSystem();
    void Clear();
}

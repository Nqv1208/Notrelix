namespace Notrelix.Application.Common.Context;

public interface ICurrentAccount
{
    Guid? AccountId { get; }
    bool IsSet { get; }
    bool HasAccount => AccountId.HasValue;
    void SetAccount(Guid accountId);
}

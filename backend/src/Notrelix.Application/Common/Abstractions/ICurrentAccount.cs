namespace Notrelix.Application.Common.Abstractions;

public interface ICurrentAccount
{
    Guid? AccountId { get; }
    bool IsSet { get; }
    bool HasAccount => AccountId.HasValue;
    void SetAccount(Guid accountId);
}

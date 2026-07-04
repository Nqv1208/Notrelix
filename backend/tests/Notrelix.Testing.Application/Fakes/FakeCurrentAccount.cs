
namespace Notrelix.Testing.Application.Fakes;

public class FakeCurrentAccount : ICurrentAccount
{
    public Guid? AccountId { get; set; }
    public bool IsSet => AccountId.HasValue;

    public void SetAccount(Guid accountId)
    {
        AccountId = accountId;
    }
}

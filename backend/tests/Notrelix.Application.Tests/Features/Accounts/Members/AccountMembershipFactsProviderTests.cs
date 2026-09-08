using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Tests.Features.Identity;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Tests.Features.Accounts.Members;

public class AccountMembershipFactsProviderTests : IdentityHandlerTestBase
{
    private readonly AccountMembershipFactsProvider _sut;

    public AccountMembershipFactsProviderTests()
    {
        _sut = new AccountMembershipFactsProvider(AccountContextMock.Object);
    }

    [Fact]
    public async Task GetAdmission_WhenAccountMissing_ReturnsNull()
    {
        var fact = await _sut.GetAdmissionAsync(Guid.NewGuid(), CancellationToken.None);

        fact.Should().BeNull();
    }

    [Theory]
    [InlineData(AccountStatus.Active, true)]
    [InlineData(AccountStatus.Trialing, true)]
    [InlineData(AccountStatus.Suspended, false)]
    [InlineData(AccountStatus.Closed, false)]
    public async Task GetAdmission_MapsAccountStatus_ToAdmissionFact(AccountStatus status, bool expectedCanAdmit)
    {
        var accountId = Guid.CreateVersion7();
        var account = CreateAccount(status, accountId);
        SetupAccounts(account);

        var fact = await _sut.GetAdmissionAsync(accountId, CancellationToken.None);

        fact.Should().NotBeNull();
        fact!.CanAdmitMember.Should().Be(expectedCanAdmit);
    }

    private Account CreateAccount(AccountStatus status, Guid id)
    {
        var account = Account.Create(
            "Test Account",
            "test-account",
            AccountType.Team,
            Guid.CreateVersion7(),
            TestNow);

        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(account, id);
        typeof(Account).GetProperty(nameof(Account.Status))!.SetValue(account, status);
        return account;
    }
}

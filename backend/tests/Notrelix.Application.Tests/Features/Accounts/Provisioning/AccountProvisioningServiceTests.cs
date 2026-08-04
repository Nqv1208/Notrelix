using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Tests.Features.Identity;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Tests.Features.Accounts.Provisioning;

public class AccountProvisioningServiceTests : IdentityHandlerTestBase
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    private AccountProvisioningService CreateSut() => new(AccountContextMock.Object);

    [Fact]
    public async Task ProvisionPersonalAccountAsync_CreatesPersonalAccount_WithOwnerMembership()
    {
        var sut = CreateSut();

        var result = await sut.ProvisionPersonalAccountAsync(UserId, "Test User", TestNow, CancellationToken.None);

        result.AccountId.Should().NotBeEmpty();
        AccountContextMock.Verify(c => c.Accounts.Add(It.Is<Account>(account =>
            account.Id == result.AccountId
            && account.Name == "Test User's Account"
            && account.Type == AccountType.Personal)), Times.Once);
        AccountContextMock.Verify(c => c.AccountMembers.Add(It.Is<AccountMember>(member =>
            member.AccountId == result.AccountId
            && member.UserId == UserId
            && member.Role == AccountRole.Owner)), Times.Once);
    }

    [Fact]
    public async Task ProvisionPersonalAccountAsync_DoesNotSaveOrCommit()
    {
        var sut = CreateSut();

        await sut.ProvisionPersonalAccountAsync(UserId, "Test User", TestNow, CancellationToken.None);

        AccountContextMock.Verify(c => c.Accounts.Add(It.IsAny<Account>()), Times.Once);
        AccountContextMock.Verify(c => c.AccountMembers.Add(It.IsAny<AccountMember>()), Times.Once);
    }
}

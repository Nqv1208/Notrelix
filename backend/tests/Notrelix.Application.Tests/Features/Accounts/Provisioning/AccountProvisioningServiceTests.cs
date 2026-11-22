using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Tests.Features.Identity;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Tests.Features.Accounts.Provisioning;

public class AccountProvisioningServiceTests : IdentityHandlerTestBase
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    private AccountProvisioningService CreateSut() => new(AccountContextMock.Object, GrantProjectionMock.Object);

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
        GrantProjectionMock.Verify(g => g.SyncAccountMemberGrantAsync(
            result.AccountId, UserId, AccountRole.Owner, TestNow, CancellationToken.None), Times.Once);
    }

    private async Task<Account> ProvisionAsync(string displayName)
    {
        Account? added = null;
        AccountContextMock.Setup(c => c.Accounts.Add(It.IsAny<Account>()))
            .Callback<Account>(account => added = account);

        await CreateSut().ProvisionPersonalAccountAsync(UserId, displayName, TestNow, CancellationToken.None);

        return added!;
    }

    [Fact]
    public async Task ProvisionPersonalAccountAsync_SlugIsTheNamesSlugWithARandomTail()
    {
        var account = await ProvisionAsync("Minh An");

        account.Slug.Should().MatchRegex("^minh-ans-account-[0-9a-f]{16}$");
    }

    [Fact]
    public async Task ProvisionPersonalAccountAsync_TwoPeopleWithTheSameDisplayNameGetDifferentSlugs()
    {
        var first = await ProvisionAsync("Minh An");
        var second = await ProvisionAsync("Minh An");

        first.Slug.Should().NotBe(second.Slug);
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task ProvisionPersonalAccountAsync_ANameWithNoLatinLettersStillYieldsAValidSlug()
    {
        var account = await ProvisionAsync("李明");

        account.Slug.Should().MatchRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        account.Slug.Should().MatchRegex("-[0-9a-f]{16}$");
    }

    [Fact]
    public async Task ProvisionPersonalAccountAsync_ADisplayNameAtTheLongestAllowedDoesNotFailRegistration()
    {
        var account = await ProvisionAsync(new string('a', Notrelix.Domain.Identity.Users.User.MaxNameLength));

        account.Name.Length.Should().BeLessThanOrEqualTo(160);
        account.Name.Should().EndWith("'s Account");
        account.Slug.Length.Should().BeLessThanOrEqualTo(128);
        account.Slug.Should().MatchRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$");
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

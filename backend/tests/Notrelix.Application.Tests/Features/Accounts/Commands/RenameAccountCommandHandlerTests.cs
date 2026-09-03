using Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;
using Notrelix.Application.Tests.Features.Identity;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Tests.Features.Accounts.Commands;

public class RenameAccountCommandHandlerTests : IdentityHandlerTestBase
{
    private readonly Guid _testAccountId = Guid.CreateVersion7();

    private RenameAccountCommandHandler CreateSut() => new(
        AccountContextMock.Object,
        RequestContextMock.Object,
        DateTimeProviderMock.Object);

    private Account CreateTestAccount(string name = "Acme Inc")
    {
        var account = Account.Create(
            name,
            "acme-inc",
            AccountType.Personal,
            TestUserId,
            TestNow);
        account.GetType().GetProperty(nameof(Account.Id))!.SetValue(account, _testAccountId);
        return account;
    }

    [Fact]
    public async Task Handle_WhenAccountExists_RenamesAccount()
    {
        var account = CreateTestAccount();
        SetupAccounts(account);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(_testAccountId);

        var sut = CreateSut();
        var result = await sut.Handle(new RenameAccountCommand("Acme Renamed"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        account.Name.Should().Be("Acme Renamed");
    }

    [Fact]
    public async Task Handle_WhenSameName_IsNoOp()
    {
        var account = CreateTestAccount("Acme Inc");
        SetupAccounts(account);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(_testAccountId);
        var originalVersion = account.Version;

        var sut = CreateSut();
        var result = await sut.Handle(new RenameAccountCommand("Acme Inc"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        account.Name.Should().Be("Acme Inc");
        account.Version.Should().Be(originalVersion);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ThrowsNotFound()
    {
        SetupAccounts();
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(_testAccountId);

        var sut = CreateSut();
        var act = () => sut.Handle(new RenameAccountCommand("Acme Renamed"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

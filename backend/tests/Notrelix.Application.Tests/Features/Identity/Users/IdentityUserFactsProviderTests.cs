using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Users;

public class IdentityUserFactsProviderTests : IdentityHandlerTestBase
{
    private readonly IdentityUserFactsProvider _sut;

    public IdentityUserFactsProviderTests()
    {
        _sut = new IdentityUserFactsProvider(IdentityContextMock.Object);
    }

    [Fact]
    public async Task FindById_WhenUserMissing_ReturnsNull()
    {
        var fact = await _sut.FindByIdAsync(Guid.NewGuid(), CancellationToken.None);

        fact.Should().BeNull();
    }

    [Theory]
    [InlineData(UserStatus.Active, true)]
    [InlineData(UserStatus.PendingVerification, true)]
    [InlineData(UserStatus.Inactive, false)]
    [InlineData(UserStatus.Suspended, false)]
    public async Task FindById_MapsLifecycleStatus_ToParticipationFact(UserStatus status, bool expectedCanParticipate)
    {
        var userId = Guid.CreateVersion7();
        SetupUsers(CreateUser(
            email: "member@example.com",
            status: status,
            emailConfirmed: true,
            id: userId));

        var fact = await _sut.FindByIdAsync(userId, CancellationToken.None);

        fact.Should().NotBeNull();
        fact!.UserId.Should().Be(userId);
        fact.Email.Should().NotBeNullOrWhiteSpace();
        fact.EmailConfirmed.Should().BeTrue();
        fact.CanParticipate.Should().Be(expectedCanParticipate);
    }

    [Fact]
    public async Task FindById_ProjectsUnconfirmedEmailFlag()
    {
        var userId = Guid.CreateVersion7();
        SetupUsers(CreateUser(emailConfirmed: false, id: userId));

        var fact = await _sut.FindByIdAsync(userId, CancellationToken.None);

        fact.Should().NotBeNull();
        fact!.EmailConfirmed.Should().BeFalse();
        fact.CanParticipate.Should().BeTrue("PendingVerification and Active users may participate");
    }
}

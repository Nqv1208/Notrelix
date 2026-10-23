using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetAccountPreferences;
using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Queries;

public class GetAccountPreferencesTests : IdentityHandlerTestBase
{
    private GetAccountPreferencesQueryHandler CreateSut() => new(IdentityContextMock.Object, RequestContextMock.Object);

    [Fact]
    public async Task Handle_WhenNothingSaved_ReturnsDefaultsAtVersionZero()
    {
        SetupUserProfiles();

        var result = await CreateSut().Handle(new GetAccountPreferencesQuery(), CancellationToken.None);

        result.Data.Should().Be(AccountPreferencesDto.Defaults);
        result.Data!.Version.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyTheCallersOwnProfile()
    {
        var mine = UserProfile.Create(TestUserId, TestNow);
        mine.ApplyAccountPreferences("en", "Asia/Tokyo", "dark", TestNow);
        var theirs = UserProfile.Create(Guid.CreateVersion7(), TestNow);
        theirs.ApplyAccountPreferences("fr", "Europe/Paris", "light", TestNow);
        SetupUserProfiles(theirs, mine);

        var result = await CreateSut().Handle(new GetAccountPreferencesQuery(), CancellationToken.None);

        result.Data.Should().Be(new AccountPreferencesDto("en", "Asia/Tokyo", "dark", mine.Version));
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Profiles.Ports;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetUserAvatar;
using Notrelix.Application.Features.Identity.Profiles.Services;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Queries;

public sealed class GetUserAvatarTests : IdentityHandlerTestBase
{
    private readonly Mock<IAvatarAudienceReadPort> _audience = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Guid _ownerId = Guid.CreateVersion7();

    private GetUserAvatarQueryHandler Sut() => new(
        IdentityContextMock.Object, RequestContextMock.Object, _audience.Object, _storage.Object,
        NullLogger<GetUserAvatarQueryHandler>.Instance);

    private User OwnerWithPhoto(string? key = null)
    {
        var owner = CreateUser(email: "owner@test.com", id: _ownerId);
        key ??= AvatarStorageKey.Create(_ownerId, new AvatarFormat("image/webp", "webp"));
        owner.ChangeUploadedAvatar(key, AvatarStorageKey.UrlFor(_ownerId, key), _ownerId, TestNow);
        _storage.Setup(s => s.OpenReadAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));
        return owner;
    }

    [Fact]
    public async Task TheOwner_ReadsOwnPhoto_WithoutAnAudienceCheck()
    {
        RequestContextMock.Setup(c => c.UserId).Returns(_ownerId);
        SetupUsers(OwnerWithPhoto());

        var result = await Sut().Handle(new GetUserAvatarQuery(_ownerId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.ContentType.Should().Be("image/webp");
        _audience.Verify(a => a.ShareActiveWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task APersonWhoSharesAWorkspace_ReadsThePhoto()
    {
        SetupUsers(OwnerWithPhoto());
        _audience.Setup(a => a.ShareActiveWorkspaceAsync(TestUserId, _ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().Handle(new GetUserAvatarQuery(_ownerId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ANeighbourWhoSharesNothing_GetsNotFound_AndNoObjectIsOpened()
    {
        SetupUsers(OwnerWithPhoto());
        _audience.Setup(a => a.ShareActiveWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var read = () => Sut().Handle(new GetUserAvatarQuery(_ownerId), CancellationToken.None);

        await read.Should().ThrowAsync<NotFoundException>();
        _storage.Verify(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AUserWithoutAnUploadedPhoto_IsIndistinguishableFromAnUnknownUser()
    {
        SetupUsers(CreateUser(email: "owner@test.com", id: _ownerId));
        _audience.Setup(a => a.ShareActiveWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var none = () => Sut().Handle(new GetUserAvatarQuery(_ownerId), CancellationToken.None);
        var unknown = () => Sut().Handle(new GetUserAvatarQuery(Guid.NewGuid()), CancellationToken.None);

        await none.Should().ThrowAsync<NotFoundException>();
        await unknown.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AKeyOutsideTheOwnerPrefix_IsNeverOpened()
    {
        var foreignKey = AvatarStorageKey.Create(Guid.NewGuid(), new AvatarFormat("image/png", "png"));
        SetupUsers(OwnerWithPhoto(foreignKey));
        RequestContextMock.Setup(c => c.UserId).Returns(_ownerId);

        var read = () => Sut().Handle(new GetUserAvatarQuery(_ownerId), CancellationToken.None);

        await read.Should().ThrowAsync<NotFoundException>();
        _storage.Verify(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

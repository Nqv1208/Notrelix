using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserAvatarTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    private static User NewUser() => User.Create("avatar@test.com", "Avatar", "hash", Now, hasPasswordCredential: true);

    [Fact]
    public void ChangeUploadedAvatar_SetsAddressAndKey_AsOneVersionStep_AndReturnsNoReplacedKey()
    {
        var user = NewUser();
        var version = user.Version;

        var replaced = user.ChangeUploadedAvatar("users/a/avatar/b.png", "/api/v1/users/a/avatar?v=b", user.Id, Later);

        replaced.Should().BeNull();
        user.AvatarUrl.Should().Be("/api/v1/users/a/avatar?v=b");
        user.AvatarStorageKey.Should().Be("users/a/avatar/b.png");
        user.Version.Should().Be(version + 1);
        user.UpdatedAt.Should().Be(Later);
        user.DomainEvents.Should().Contain(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void ChangeUploadedAvatar_ReturnsTheReplacedKey_SoItsObjectCanBeSwept()
    {
        var user = NewUser();
        user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Now);

        var replaced = user.ChangeUploadedAvatar("users/a/avatar/two.webp", "/two", user.Id, Later);

        replaced.Should().Be("users/a/avatar/one.png");
        user.AvatarStorageKey.Should().Be("users/a/avatar/two.webp");
        user.AvatarUrl.Should().Be("/two");
    }

    [Fact]
    public void ChangeUploadedAvatar_RejectsBlankKeyOrAddress_AndLeavesStateUnchanged()
    {
        var user = NewUser();
        user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Now);
        var version = user.Version;

        var blankKey = () => user.ChangeUploadedAvatar(" ", "/two", user.Id, Later);
        var blankUrl = () => user.ChangeUploadedAvatar("users/a/avatar/two.png", "", user.Id, Later);

        blankKey.Should().Throw<BusinessRuleException>();
        blankUrl.Should().Throw<BusinessRuleException>();
        user.AvatarStorageKey.Should().Be("users/a/avatar/one.png");
        user.AvatarUrl.Should().Be("/one");
        user.Version.Should().Be(version);
    }

    [Fact]
    public void RemoveAvatar_ClearsBoth_AndReturnsTheRemovedKey()
    {
        var user = NewUser();
        user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Now);
        var version = user.Version;

        var removed = user.RemoveAvatar(user.Id, Later);

        removed.Should().Be("users/a/avatar/one.png");
        user.AvatarUrl.Should().BeNull();
        user.AvatarStorageKey.Should().BeNull();
        user.Version.Should().Be(version + 1);
    }

    [Fact]
    public void RemoveAvatar_WhenThereIsNone_IsASemanticNoOp()
    {
        var user = NewUser();
        var version = user.Version;
        var events = user.DomainEvents.Count;

        var removed = user.RemoveAvatar(user.Id, Later);

        removed.Should().BeNull();
        user.Version.Should().Be(version);
        user.DomainEvents.Count.Should().Be(events);
    }

    [Fact]
    public void RemoveAvatar_ClearsAnExternalAddressToo()
    {
        var user = NewUser();
        user.UpdateProfile("Avatar", "https://cdn.example.com/a.png", user.Id, Now);

        var removed = user.RemoveAvatar(user.Id, Later);

        removed.Should().BeNull();
        user.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WithoutAnAvatar_NeverErasesAnUploadedPhoto()
    {
        var user = NewUser();
        user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Now);

        user.UpdateProfile("Renamed", null, user.Id, Later);

        user.Name.Should().Be("Renamed");
        user.AvatarUrl.Should().Be("/one");
        user.AvatarStorageKey.Should().Be("users/a/avatar/one.png");
    }

    [Fact]
    public void UpdateProfile_WithAnExternalAddress_ReplacesTheUploadedPhotoAndDropsItsKey()
    {
        var user = NewUser();
        user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Now);

        user.UpdateProfile("Avatar", "https://cdn.example.com/a.png", user.Id, Later);

        user.AvatarUrl.Should().Be("https://cdn.example.com/a.png");
        user.AvatarStorageKey.Should().BeNull();
    }

    [Fact]
    public void ChangeUploadedAvatar_OnADeletedUser_IsRejected()
    {
        var user = NewUser();
        user.Delete(user.Id, Now);

        var change = () => user.ChangeUploadedAvatar("users/a/avatar/one.png", "/one", user.Id, Later);

        change.Should().Throw<BusinessRuleException>();
    }
}

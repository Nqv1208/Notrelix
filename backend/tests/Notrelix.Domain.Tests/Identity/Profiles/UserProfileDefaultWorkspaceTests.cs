using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserProfileDefaultWorkspaceTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 24, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddHours(1);

    private static UserProfile NewProfile() => UserProfile.Create(Guid.NewGuid(), CreatedAt);

    [Fact]
    public void ANewProfileHasNoDefaultWorkspace()
    {
        NewProfile().DefaultWorkspaceId.Should().BeNull();
    }

    [Fact]
    public void ChangeDefaultWorkspace_Sets_AsOneVersionStep_WithAnEvent()
    {
        var profile = NewProfile();
        var workspaceId = Guid.NewGuid();
        var version = profile.Version;

        var changed = profile.ChangeDefaultWorkspace(workspaceId, Later);

        changed.Should().BeTrue();
        profile.DefaultWorkspaceId.Should().Be(workspaceId);
        profile.Version.Should().Be(version + 1);
        profile.UpdatedAt.Should().Be(Later);
        profile.DomainEvents.Should().Contain(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void ChangeDefaultWorkspace_ToTheSameValue_IsASemanticNoOp()
    {
        var profile = NewProfile();
        var workspaceId = Guid.NewGuid();
        profile.ChangeDefaultWorkspace(workspaceId, CreatedAt);
        var version = profile.Version;
        var events = profile.DomainEvents.Count;

        var changed = profile.ChangeDefaultWorkspace(workspaceId, Later);

        changed.Should().BeFalse();
        profile.Version.Should().Be(version);
        profile.DomainEvents.Count.Should().Be(events);
    }

    [Fact]
    public void ChangeDefaultWorkspace_ToNull_Clears()
    {
        var profile = NewProfile();
        profile.ChangeDefaultWorkspace(Guid.NewGuid(), CreatedAt);

        var changed = profile.ChangeDefaultWorkspace(null, Later);

        changed.Should().BeTrue();
        profile.DefaultWorkspaceId.Should().BeNull();
    }

    [Fact]
    public void ChangeDefaultWorkspace_ClearingAnAbsentDefault_IsASemanticNoOp()
    {
        var profile = NewProfile();
        var version = profile.Version;

        profile.ChangeDefaultWorkspace(null, Later).Should().BeFalse();

        profile.Version.Should().Be(version);
    }

    [Fact]
    public void ChangeDefaultWorkspace_RejectsAnEmptyId_AndLeavesStateUnchanged()
    {
        var profile = NewProfile();
        var workspaceId = Guid.NewGuid();
        profile.ChangeDefaultWorkspace(workspaceId, CreatedAt);
        var version = profile.Version;

        var change = () => profile.ChangeDefaultWorkspace(Guid.Empty, Later);

        change.Should().Throw<BusinessRuleException>();
        profile.DefaultWorkspaceId.Should().Be(workspaceId);
        profile.Version.Should().Be(version);
    }
}

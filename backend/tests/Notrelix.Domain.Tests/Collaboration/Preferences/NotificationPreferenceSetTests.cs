using FluentAssertions;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Preferences;

namespace Notrelix.Domain.Tests.Collaboration.Preferences;

/// <summary>
/// The notification preference matrix (accepted 2026-10-23, owned by Collaboration): defaults apply to every
/// cell without an explicit choice, a change is all-or-nothing, setting a value that already holds is a
/// semantic no-op, and unknown types or channels are rejected without partial change.
/// </summary>
public class NotificationPreferenceSetTests
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    private static NotificationPreferenceChange Change(string type, string channel, bool enabled) => new(type, channel, enabled);

    private static NotificationPreferenceSet NewSet(params NotificationPreferenceChange[] changes) =>
        NotificationPreferenceSet.Create(AccountId, WorkspaceId, UserId, changes, Now)!;

    [Theory]
    [InlineData("assigned", true, true)]
    [InlineData("mention", true, true)]
    [InlineData("approval", true, true)]
    [InlineData("invitation", true, true)]
    [InlineData("overdue", true, false)]
    [InlineData("watching", true, false)]
    [InlineData("digest", true, false)]
    public void Defaults_AreInAppOnForAllAndEmailOnForTheFourDirectTypes(string type, bool inApp, bool email)
    {
        NotificationPreferenceDefaults.IsEnabled(type, NotificationChannels.InApp).Should().Be(inApp);
        NotificationPreferenceDefaults.IsEnabled(type, NotificationChannels.Email).Should().Be(email);
    }

    [Fact]
    public void Vocabulary_IsTheSevenAcceptedTypesAndTwoChannels()
    {
        NotificationTypes.All.Should().Equal("assigned", "mention", "approval", "overdue", "watching", "invitation", "digest");
        NotificationChannels.All.Should().Equal("in_app", "email");
    }

    [Fact]
    public void MentionCreatedLiteral_MapsToTheMentionType_AndNothingElseMaps()
    {
        NotificationTypes.FromProducedType("mention.created").Should().Be(NotificationTypes.Mention);
        NotificationTypes.FromProducedType("comment.created").Should().BeNull();
        NotificationTypes.FromProducedType(null).Should().BeNull();
    }

    [Fact]
    public void Create_WithOnlyDefaultValues_CreatesNothing()
    {
        var set = NotificationPreferenceSet.Create(AccountId, WorkspaceId, UserId,
            [Change("mention", "in_app", true), Change("digest", "email", false)], Now);

        set.Should().BeNull("every requested value already equals its default");
    }

    [Fact]
    public void Create_StoresOnlyTheValuesThatDifferFromTheirDefault_AtVersionOne()
    {
        var set = NewSet(Change("mention", "in_app", false), Change("digest", "email", true), Change("assigned", "email", true));

        set.Version.Should().Be(1);
        set.AccountId.Should().Be(AccountId);
        set.WorkspaceId.Should().Be(WorkspaceId);
        set.UserId.Should().Be(UserId);
        set.CreatedAt.Should().Be(Now);
        set.Entries.Should().HaveCount(2, "assigned/email already holds, so no entry is kept for it");
        set.IsEnabled("mention", "in_app").Should().BeFalse();
        set.IsEnabled("digest", "email").Should().BeTrue();
        set.IsEnabled("assigned", "email").Should().BeTrue();
        set.IsEnabled("watching", "in_app").Should().BeTrue("untouched cells keep their default");
        set.DomainEvents.Should().BeEmpty("a preference is personal state and publishes no business fact");
    }

    [Fact]
    public void Apply_Toggles_AndTogglingBackKeepsOneEntryPerCell()
    {
        var set = NewSet(Change("mention", "in_app", false));

        set.Apply([Change("mention", "in_app", true)], Now.AddMinutes(1)).Should().BeTrue();

        set.IsEnabled("mention", "in_app").Should().BeTrue();
        set.Entries.Should().ContainSingle();
        set.Version.Should().Be(2);
        set.UpdatedAt.Should().Be(Now.AddMinutes(1));
        set.UpdatedBy.Should().Be(UserId);
    }

    [Fact]
    public void Apply_AValueThatAlreadyHolds_IsASemanticNoOp()
    {
        var set = NewSet(Change("mention", "in_app", false));

        var changed = set.Apply([Change("mention", "in_app", false), Change("overdue", "email", false)], Now.AddMinutes(1));

        changed.Should().BeFalse();
        set.Version.Should().Be(1);
        set.UpdatedAt.Should().BeNull();
        set.Entries.Should().ContainSingle();
    }

    [Fact]
    public void Apply_IsAllOrNothing_AnUnknownTypeLeavesEverythingUnchanged()
    {
        var set = NewSet(Change("mention", "in_app", false));

        var act = () => set.Apply([Change("overdue", "email", true), Change("nonsense", "email", true)], Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownType);
        set.IsEnabled("overdue", "email").Should().BeFalse();
        set.Version.Should().Be(1);
        set.Entries.Should().ContainSingle();
    }

    [Fact]
    public void Apply_AnUnknownChannel_IsRejected()
    {
        var set = NewSet(Change("mention", "in_app", false));

        var act = () => set.Apply([Change("mention", "mobile", true)], Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownChannel);
        set.Version.Should().Be(1);
    }

    [Fact]
    public void Create_AnUnknownTypeOrChannel_IsRejected()
    {
        var type = () => NotificationPreferenceSet.Create(AccountId, WorkspaceId, UserId, [Change("Mention", "in_app", false)], Now);
        var channel = () => NotificationPreferenceSet.Create(AccountId, WorkspaceId, UserId, [Change("mention", "push", false)], Now);

        type.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownType);
        channel.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownChannel);
    }

    [Fact]
    public void Apply_TheSameCellTwiceWithDifferentValues_IsRejected_ButWithTheSameValueIsFine()
    {
        var set = NewSet(Change("mention", "in_app", false));

        var conflicting = () => set.Apply([Change("overdue", "email", true), Change("overdue", "email", false)], Now.AddMinutes(1));
        var same = () => set.Apply([Change("overdue", "email", true), Change("overdue", "email", true)], Now.AddMinutes(1));

        conflicting.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_ConflictingChange);
        same.Should().NotThrow();
        set.IsEnabled("overdue", "email").Should().BeTrue();
    }

    [Fact]
    public void Apply_NoChanges_IsRejected()
    {
        var set = NewSet(Change("mention", "in_app", false));

        var act = () => set.Apply([], Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_NotificationPreference_NoChanges);
    }

    [Fact]
    public void Create_RejectsEmptyScope()
    {
        var change = new[] { Change("mention", "in_app", false) };

        ((Action)(() => NotificationPreferenceSet.Create(Guid.Empty, WorkspaceId, UserId, change, Now))).Should().Throw<Exception>();
        ((Action)(() => NotificationPreferenceSet.Create(AccountId, Guid.Empty, UserId, change, Now))).Should().Throw<Exception>();
        ((Action)(() => NotificationPreferenceSet.Create(AccountId, WorkspaceId, Guid.Empty, change, Now))).Should().Throw<Exception>();
    }
}

using FluentAssertions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Domain.Tests.Workspaces.Onboarding;

/// <summary>
/// Onboarding draft (accepted 2026-10-23: owned by Workspaces, keyed by user, 30-day TTL) and the
/// getting-started dismissal (per user and workspace). Invalid input must leave the draft unchanged.
/// </summary>
public class OnboardingDraftTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StoresStepDataAndExpiresThirtyDaysAfterTheSave()
    {
        var workspaceId = Guid.CreateVersion7();

        var draft = OnboardingDraft.Create(UserId, "  departments ", workspaceId, "{\"a\":1}", Now);

        draft.UserId.Should().Be(UserId);
        draft.Step.Should().Be("departments");
        draft.WorkspaceId.Should().Be(workspaceId);
        draft.Data.Should().Be("{\"a\":1}");
        draft.CreatedAt.Should().Be(Now);
        draft.UpdatedAt.Should().Be(Now);
        draft.ExpiresAt.Should().Be(Now.AddDays(30));
        draft.DomainEvents.Should().BeEmpty("a draft is personal state and publishes no business fact");
    }

    [Fact]
    public void Create_WithoutDataOrWorkspace_StoresAnEmptyObjectAndNoWorkspace()
    {
        var draft = OnboardingDraft.Create(UserId, "company", Guid.Empty, null, Now);

        draft.Data.Should().Be("{}");
        draft.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public void Replace_RestartsTheThirtyDaysAndKeepsTheCreationTime()
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{}", Now);
        var later = Now.AddDays(10);

        draft.Replace("invites", null, "{\"b\":2}", later);

        draft.Step.Should().Be("invites");
        draft.Data.Should().Be("{\"b\":2}");
        draft.CreatedAt.Should().Be(Now);
        draft.UpdatedAt.Should().Be(later);
        draft.ExpiresAt.Should().Be(later.AddDays(30));
    }

    [Fact]
    public void IsExpiredAt_IsTrueFromTheExpiryInstant()
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{}", Now);

        draft.IsExpiredAt(Now.AddDays(30).AddTicks(-1)).Should().BeFalse();
        draft.IsExpiredAt(Now.AddDays(30)).Should().BeTrue();
    }

    [Theory]
    [InlineData("", WorkspaceRuleCodes.Workspaces_Onboarding_DraftStepInvalid)]
    [InlineData("   ", WorkspaceRuleCodes.Workspaces_Onboarding_DraftStepInvalid)]
    [InlineData("bad\nstep", WorkspaceRuleCodes.Workspaces_Onboarding_DraftStepInvalid)]
    public void Replace_WithAnInvalidStep_IsRejectedAndChangesNothing(string step, string code)
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{\"a\":1}", Now);

        var act = () => draft.Replace(step, null, "{}", Now.AddDays(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(code);
        (draft.Step, draft.Data, draft.UpdatedAt).Should().Be(("company", "{\"a\":1}", Now));
    }

    [Fact]
    public void Replace_WithAStepOverSixtyFourCharacters_IsRejected()
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{}", Now);

        var act = () => draft.Replace(new string('x', OnboardingDraft.MaxStepLength + 1), null, "{}", Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Onboarding_DraftStepInvalid);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("{not json")]
    public void Replace_WithDataThatIsNotAJsonObject_IsRejectedAndChangesNothing(string data)
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{\"a\":1}", Now);

        var act = () => draft.Replace("company", null, data, Now.AddDays(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Onboarding_DraftDataInvalid);
        draft.Data.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void Replace_WithDataOverTheLimit_IsRejected()
    {
        var draft = OnboardingDraft.Create(UserId, "company", null, "{}", Now);
        var big = "{\"k\":\"" + new string('x', OnboardingDraft.MaxDataLength) + "\"}";

        var act = () => draft.Replace("company", null, big, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Onboarding_DraftDataTooLarge);
    }

    [Fact]
    public void Dismissal_StoresTheMemberScope()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();

        var dismissal = GettingStartedDismissal.Create(accountId, workspaceId, UserId, Now);

        (dismissal.AccountId, dismissal.WorkspaceId, dismissal.UserId, dismissal.DismissedAt)
            .Should().Be((accountId, workspaceId, UserId, Now));
    }

    [Theory]
    [InlineData("acme", true)]
    [InlineData("  Acme-Corp-2 ", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("-acme", false)]
    [InlineData("acme--corp", false)]
    [InlineData("acme corp", false)]
    [InlineData("acmé", false)]
    public void Slug_IsValid_FollowsTheWorkspaceSlugFormat(string? value, bool expected) =>
        Slug.IsValid(value).Should().Be(expected);

    [Fact]
    public void Slug_IsValid_RejectsMoreThanTheStoredLength() =>
        Slug.IsValid(new string('a', Slug.MaxLength + 1)).Should().BeFalse();
}

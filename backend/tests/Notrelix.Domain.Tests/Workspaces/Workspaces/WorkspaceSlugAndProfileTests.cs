using FluentAssertions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceSlugAndProfileTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ReservedList_IsExactlyTheAcceptedWords() =>
        WorkspaceSlugRules.Reserved.Should().BeEquivalentTo(
            "admin", "api", "app", "www", "settings", "login", "signup", "billing", "support", "help",
            "new", "invite", "join", "onboarding", "workspaces");

    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("app")]
    [InlineData("www")]
    [InlineData("settings")]
    [InlineData("login")]
    [InlineData("signup")]
    [InlineData("billing")]
    [InlineData("support")]
    [InlineData("help")]
    [InlineData("new")]
    [InlineData("invite")]
    [InlineData("join")]
    [InlineData("onboarding")]
    [InlineData("workspaces")]
    [InlineData("  Admin ")]
    public void Create_ReservedSlug_IsRejectedWithItsRuleCode(string slug)
    {
        var act = () => Workspace.Create(AccountId, OwnerId, "Name", slug, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_SlugReserved);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Create_SlugShorterThanThree_IsRejected(string slug)
    {
        var act = () => Workspace.Create(AccountId, OwnerId, "Name", slug, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_SlugTooShort);
    }

    [Fact]
    public void Create_SlugOfSixtyFourCharacters_IsRejected()
    {
        var act = () => Workspace.Create(AccountId, OwnerId, "Name", new string('a', 64), Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_SlugTooLong);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("acme-corp")]
    [InlineData("admin-team")]
    public void Create_SlugAtTheLowerBoundOrNotReserved_IsAccepted(string slug) =>
        Workspace.Create(AccountId, OwnerId, "Name", slug, Now).Slug.Should().Be(slug);

    [Fact]
    public void Create_SlugOfSixtyThreeCharacters_IsAccepted() =>
        Workspace.Create(AccountId, OwnerId, "Name", new string('a', 63), Now).Slug.Should().HaveLength(63);

    [Theory]
    [InlineData("Not Valid")]
    [InlineData("-abc")]
    [InlineData("abc--def")]
    public void Create_SlugOutsideTheFormat_KeepsTheSharedFormatCode(string slug)
    {
        var act = () => Workspace.Create(AccountId, OwnerId, "Name", slug, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(SharedKernelRuleCodes.SharedKernel_Slug_InvalidFormat);
    }

    [Theory]
    [InlineData("ab", "Workspaces_Workspace_SlugTooShort")]
    [InlineData("admin", "Workspaces_Workspace_SlugReserved")]
    [InlineData("Bad Slug", "SharedKernel_Slug_InvalidFormat")]
    [InlineData("good-slug", null)]
    public void FindViolation_NamesTheBrokenRule(string slug, string? expected) =>
        WorkspaceSlugRules.FindViolation(slug).Should().Be(expected);

    [Fact]
    public void Rename_DoesNotRevalidateTheStoredSlug()
    {
        // A workspace whose address predates the rule keeps working: only choosing a slug is checked.
        var workspace = Workspace.Create(AccountId, OwnerId, "Name", "valid-slug", Now);

        workspace.Rename("Another name", OwnerId, Now);

        workspace.Slug.Should().Be("valid-slug");
    }

    [Fact]
    public void Create_WithSizeAndLanguage_StoresThem()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Name", "acme", Now, size: "51-200", language: "EN");

        workspace.Settings.Size.Should().Be("51-200");
        workspace.Settings.Language.Should().Be("en");
    }

    [Fact]
    public void Create_WithoutSizeOrLanguage_LeavesThemUnset()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Name", "acme", Now);

        workspace.Settings.Size.Should().BeNull();
        workspace.Settings.Language.Should().BeNull();
    }

    [Theory]
    [InlineData("1-10")]
    [InlineData("11-50")]
    [InlineData("51-200")]
    [InlineData("201-1000")]
    [InlineData("1000+")]
    public void Size_EveryAcceptedBand_IsValid(string size) =>
        WorkspaceSettings.Create(size: size).Size.Should().Be(size);

    [Theory]
    [InlineData("small")]
    [InlineData("1-5")]
    [InlineData("1001")]
    public void Size_OutsideTheEnum_IsRejected(string size)
    {
        var act = () => WorkspaceSettings.Create(size: size);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Settings_SizeInvalid);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("vietnamese")]
    public void Language_OutsideViOrEn_IsRejected(string language)
    {
        var act = () => WorkspaceSettings.Create(language: language);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Settings_LanguageInvalid);
    }

    [Fact]
    public void UpdateSettings_ChangingSizeOrLanguage_BumpsTheVersionOnce_AndTheSameValuesAreANoOp()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Name", "acme", Now);
        var before = workspace.Version;

        workspace.UpdateSettings(WorkspaceSettings.Create(size: "11-50", language: "vi"), OwnerId, Now);
        workspace.Version.Should().Be(before + 1);

        workspace.UpdateSettings(WorkspaceSettings.Create(size: "11-50", language: "vi"), OwnerId, Now);
        workspace.Version.Should().Be(before + 1);
    }

    [Theory]
    [InlineData("Asia/Singapore", "Asia/Singapore")]
    [InlineData("asia/tokyo", "Asia/Tokyo")]
    [InlineData("America/New_York", "America/New_York")]
    [InlineData("Europe/Paris", "Europe/Paris")]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    public void TimeZone_AcceptedOrUnset_IsNormalised(string? timeZone, string? expected) =>
        WorkspaceSettings.Create(timeZone: timeZone).TimeZone.Should().Be(expected);

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("GMT+7")]
    [InlineData("Pacific Standard Time")]
    public void TimeZone_NotAnIanaId_IsRejected(string timeZone)
    {
        var act = () => WorkspaceSettings.Create(timeZone: timeZone);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Settings_TimeZoneInvalid);
    }

    [Theory]
    [InlineData("monday", "monday")]
    [InlineData("SUNDAY", "sunday")]
    public void WeekStart_MondayOrSunday_IsAccepted(string weekStart, string expected) =>
        WorkspaceSettings.Create(weekStart: weekStart).WeekStart.Should().Be(expected);

    [Fact]
    public void WeekStart_AnotherDay_IsRejected()
    {
        var act = () => WorkspaceSettings.Create(weekStart: "friday");

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Settings_WeekStartInvalid);
    }
}

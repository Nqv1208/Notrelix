using DomainRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Domain.Workspaces;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class CreateWorkspaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private CreateWorkspaceCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object);

    [Fact]
    public async Task Handle_NameThatDerivesTheSlug_StoresTheDerivedSlug()
    {
        var result = await CreateSut().Handle(new CreateWorkspaceCommand("Acme Corp", null, false), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        DbContextMock.Object.Workspaces.Single().Slug.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData("Admin", "admin-")]
    [InlineData("Help", "help-")]
    [InlineData("Ab", "ab-")]
    public async Task Handle_DerivedSlugReservedOrShort_GetsASuffixInsteadOfFailing(string name, string prefix)
    {
        var result = await CreateSut().Handle(new CreateWorkspaceCommand(name, null, false), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var slug = DbContextMock.Object.Workspaces.Single().Slug;
        slug.Should().StartWith(prefix);
        WorkspaceSlugRules.FindViolation(slug).Should().BeNull();
    }

    [Fact]
    public async Task Handle_VeryLongName_DerivesASlugOfAtMostSixtyThreeCharacters()
    {
        var name = string.Join(" ", Enumerable.Repeat("segment", 15));

        var result = await CreateSut().Handle(new CreateWorkspaceCommand(name, null, false), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        DbContextMock.Object.Workspaces.Single().Slug.Length.Should().BeLessThanOrEqualTo(WorkspaceSlugRules.MaxLength);
    }

    [Fact]
    public async Task Handle_ChosenSlug_IsStoredNormalized()
    {
        await CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Slug: "  Acme-Vn "), CancellationToken.None);

        DbContextMock.Object.Workspaces.Single().Slug.Should().Be("acme-vn");
    }

    [Theory]
    [InlineData("admin", "Workspaces_Workspace_SlugReserved")]
    [InlineData("ab", "Workspaces_Workspace_SlugTooShort")]
    [InlineData("Bad Slug", "SharedKernel_Slug_InvalidFormat")]
    public async Task Handle_ChosenSlugBreakingTheRule_IsRejectedWithItsRuleCode(string slug, string code)
    {
        Func<Task> act = () => CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Slug: slug), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainRuleException>()).Which.RuleCode.Should().Be(code);
        DbContextMock.Object.Workspaces.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ChosenSlugTakenInTheAccount_IsAConflict_NeverSilentlyChanged()
    {
        SetupWorkspaces(Workspace.Create(TestAccountId, TestUserId, "Existing", "acme", TestNow));

        Func<Task> act = () => CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Slug: "acme"), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode
            .Should().Be(CreateWorkspaceCommandHandler.SlugTakenErrorCode);
        DbContextMock.Object.Workspaces.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ChosenSlugTakenInAnotherAccount_IsFree()
    {
        SetupWorkspaces(Workspace.Create(Guid.NewGuid(), TestUserId, "Other", "acme", TestNow));

        var result = await CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Slug: "acme"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithSizeAndLanguage_StoresThem()
    {
        await CreateSut().Handle(
            new CreateWorkspaceCommand("Acme", null, false, Size: "201-1000", Language: "vi"), CancellationToken.None);

        var settings = DbContextMock.Object.Workspaces.Single().Settings;
        settings.Size.Should().Be("201-1000");
        settings.Language.Should().Be("vi");
    }

    [Fact]
    public async Task Handle_InvalidSizeOrLanguage_IsRejected()
    {
        Func<Task> badSize = () => CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Size: "huge"), CancellationToken.None);
        Func<Task> badLanguage = () => CreateSut().Handle(new CreateWorkspaceCommand("Acme", null, false, Language: "fr"), CancellationToken.None);

        (await badSize.Should().ThrowAsync<DomainRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Settings_SizeInvalid);
        (await badLanguage.Should().ThrowAsync<DomainRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Settings_LanguageInvalid);
    }
}

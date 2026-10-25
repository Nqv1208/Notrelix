using DomainRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Domain.Workspaces;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;

namespace Notrelix.Application.Tests.Features.Workspaces.Settings.Commands;

public class UpdateWorkspaceSettingsCommandHandlerTests : WorkspaceHandlerTestBase
{
    private UpdateWorkspaceSettingsCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_UpdatesSettings()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();
        var command = new UpdateWorkspaceSettingsCommand(workspace.Id, true, false, true, "Member", 7, 1);
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new UpdateWorkspaceSettingsCommand(TestWorkspaceId, true, false, true, "Member", 7, 1);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithSizeAndLanguage_SetsThem_AndOmittingThemKeepsTheStoredValues()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();

        await sut.Handle(new UpdateWorkspaceSettingsCommand(workspace.Id, false, false, false, "Member", 7, 1, null, "11-50", "en"), CancellationToken.None);
        workspace.Settings.Size.Should().Be("11-50");
        workspace.Settings.Language.Should().Be("en");

        await sut.Handle(new UpdateWorkspaceSettingsCommand(workspace.Id, true, false, false, "Member", 7, 2), CancellationToken.None);
        workspace.Settings.AllowPublicSharing.Should().BeTrue();
        workspace.Settings.Size.Should().Be("11-50");
        workspace.Settings.Language.Should().Be("en");
    }

    [Fact]
    public async Task Handle_InvalidSize_IsRejectedAndChangesNothing()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var version = workspace.Version;

        Func<Task> act = () => CreateSut().Handle(
            new UpdateWorkspaceSettingsCommand(workspace.Id, false, false, false, "Member", 7, 1, null, "huge", null), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Settings_SizeInvalid);
        workspace.Version.Should().Be(version);
        workspace.Settings.Size.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithTimeZoneAndWeekStart_SetsThem_AndOmittingThemKeepsTheStoredValues()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();

        await sut.Handle(new UpdateWorkspaceSettingsCommand(workspace.Id, false, false, false, "Member", 7, 1, null, null, null, "Asia/Tokyo", "sunday"), CancellationToken.None);
        workspace.Settings.TimeZone.Should().Be("Asia/Tokyo");
        workspace.Settings.WeekStart.Should().Be("sunday");

        await sut.Handle(new UpdateWorkspaceSettingsCommand(workspace.Id, true, false, false, "Member", 7, 2), CancellationToken.None);
        workspace.Settings.AllowPublicSharing.Should().BeTrue();
        workspace.Settings.TimeZone.Should().Be("Asia/Tokyo");
        workspace.Settings.WeekStart.Should().Be("sunday");
    }
}

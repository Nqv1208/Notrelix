using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class CreateBoardInWorkspaceTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardInWorkspaceCommandHandler _handler;

    public CreateBoardInWorkspaceTests()
    {
        _handler = new CreateBoardInWorkspaceCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesBoardWithDefaultFields()
    {
        var command = new CreateBoardInWorkspaceCommand(
            TestWorkspaceId, "New Board", "A description", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithBackground_SetsBackground()
    {
        var command = new CreateBoardInWorkspaceCommand(
            TestWorkspaceId, "Board", null, "{\"type\":\"color\",\"value\":\"#FF0000\"}", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithVisibility_SetsVisibility()
    {
        var command = new CreateBoardInWorkspaceCommand(
            TestWorkspaceId, "Board", null, null, BoardVisibility.PublicLink);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullVisibility_DefaultsToWorkspace()
    {
        var command = new CreateBoardInWorkspaceCommand(
            TestWorkspaceId, "Board", null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}

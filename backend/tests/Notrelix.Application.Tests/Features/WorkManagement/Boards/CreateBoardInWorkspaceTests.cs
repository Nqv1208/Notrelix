using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class CreateBoardInWorkspaceTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardInWorkspaceCommandHandler _handler;

    public CreateBoardInWorkspaceTests()
    {
        _handler = new CreateBoardInWorkspaceCommandHandler(
            DbContextMock.Object,
            Mock.Of<Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement>(),
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
    public async Task Handle_SeedsTheStatusFieldWithTheDefaultWorkStatuses()
    {
        var fields = new List<BoardField>();
        var fieldSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<BoardField>>();
        fieldSet.Setup(s => s.AddRange(It.IsAny<BoardField[]>())).Callback<BoardField[]>(fields.AddRange);
        DbContextMock.Setup(c => c.BoardFields).Returns(fieldSet.Object);
        var command = new CreateBoardInWorkspaceCommand(TestWorkspaceId, "New Board", null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        var status = fields.Should().ContainSingle(f => f.Type == FieldType.Status).Subject;
        status.Options.OrderBy(o => o.Position).Select(o => (o.Name, o.Category)).Should().Equal(
            ("Cần làm", WorkStatusCategory.Todo),
            ("Đang làm", WorkStatusCategory.InProgress),
            ("Chờ duyệt", WorkStatusCategory.Review),
            ("Hoàn thành", WorkStatusCategory.Done));
        fields.Where(f => f.Type != FieldType.Status).SelectMany(f => f.Options).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreatesOneGroupPerDefaultStatusSoItemsCanBeAddedAtOnce()
    {
        var groups = new List<Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup>();
        var groupSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup>>();
        groupSet.Setup(s => s.Add(It.IsAny<Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup>()))
            .Callback<Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup>(groups.Add);
        DbContextMock.Setup(c => c.BoardGroups).Returns(groupSet.Object);
        var command = new CreateBoardInWorkspaceCommand(TestWorkspaceId, "New Board", null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        groups.OrderBy(g => g.Position).Select(g => g.Title).Should().Equal("Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành");
        groups.Should().OnlyContain(g => g.BoardId == result.Data && !g.IsArchived);
        groups.Select(g => g.Position.Value).Should().OnlyHaveUniqueItems();
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

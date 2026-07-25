using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

namespace Notrelix.Application.Tests.Features.WorkManagement.ItemLinks;

public class DeleteBoardItemLinkTests : WorkManagementHandlerTestBase
{
    private readonly DeleteBoardItemLinkCommandHandler _handler;

    public DeleteBoardItemLinkTests()
    {
        _handler = new DeleteBoardItemLinkCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidLink_DeletesLink()
    {
        var link = CreateBoardItemLink();
        SetupBoardItemLinks(link);

        var command = new DeleteBoardItemLinkCommand(link.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LinkNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBoardItemLinkCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ThrowsNotFoundException()
    {
        var link = CreateBoardItemLink();
        SetupBoardItemLinks(link);

        var command = new DeleteBoardItemLinkCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}

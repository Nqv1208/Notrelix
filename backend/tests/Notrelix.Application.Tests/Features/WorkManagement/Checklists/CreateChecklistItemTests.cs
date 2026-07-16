using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class CreateChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly CreateChecklistItemCommandHandler _handler;

    public CreateChecklistItemTests()
    {
        _handler = new CreateChecklistItemCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesChecklistItem()
    {
        var command = new CreateChecklistItemCommand(Guid.CreateVersion7(), "Buy milk");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNonEmptyId()
    {
        var command = new CreateChecklistItemCommand(Guid.CreateVersion7(), "Task");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data.Should().NotBe(Guid.Empty);
    }
}

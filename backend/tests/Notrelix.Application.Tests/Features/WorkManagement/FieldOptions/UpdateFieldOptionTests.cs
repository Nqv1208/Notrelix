using Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.UpdateFieldOption;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.FieldOptions;

public class UpdateFieldOptionTests : WorkManagementHandlerTestBase
{
    private readonly UpdateFieldOptionCommandHandler _handler;

    public UpdateFieldOptionTests()
    {
        _handler = new UpdateFieldOptionCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesOption()
    {
        var field = CreateBoardField(FieldType.Select);
        field.AddOption("Old Name", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var optionId = field.Options.First().Id;
        SetupBoardFields(field);

        var command = new UpdateFieldOptionCommand(field.Id, optionId, "New Name", "#00FF00");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateFieldOptionCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "New Name", "#00FF00");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OptionNotFound_ThrowsDomainNotFoundException()
    {
        var field = CreateBoardField(FieldType.Select);
        SetupBoardFields(field);

        var command = new UpdateFieldOptionCommand(field.Id, Guid.CreateVersion7(), "New Name", "#00FF00");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Application.Common.Exceptions.NotFoundException>();
    }

    private BoardField CreateBoardField(FieldType type)
    {
        return BoardField.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            "Test Field",
            type,
            FieldSettings.Empty(),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
    }
}

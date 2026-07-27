using Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.DeleteFieldOption;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.FieldOptions;

public class DeleteFieldOptionTests : WorkManagementHandlerTestBase
{
    private readonly DeleteFieldOptionCommandHandler _handler;

    public DeleteFieldOptionTests()
    {
        _handler = new DeleteFieldOptionCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDelete_RemovesOption()
    {
        var field = CreateBoardField(FieldType.Select);
        field.AddOption("Option 1", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var optionId = field.Options.First().Id;
        SetupBoardFields(field);

        var command = new DeleteFieldOptionCommand(field.Id, optionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteFieldOptionCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OptionNotFound_ThrowsDomainNotFoundException()
    {
        var field = CreateBoardField(FieldType.Select);
        SetupBoardFields(field);

        var command = new DeleteFieldOptionCommand(field.Id, Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }

    private BoardField CreateBoardField(FieldType type)
    {
        var settings = type == FieldType.Status
            ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!)
            : FieldSettings.Empty();
        return BoardField.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            "Test Field",
            type,
            settings,
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
    }
}

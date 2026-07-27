using Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.ReorderFieldOptions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.FieldOptions;

public class ReorderFieldOptionsTests : WorkManagementHandlerTestBase
{
    private readonly ReorderFieldOptionsCommandHandler _handler;

    public ReorderFieldOptionsTests()
    {
        _handler = new ReorderFieldOptionsCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReorder_ReordersOptions()
    {
        var field = CreateBoardField(FieldType.Select);
        field.AddOption("Option 1", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        field.AddOption("Option 2", Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardFields(field);

        var optionIds = field.Options.Select(o => o.Id).Reverse().ToList();
        var command = new ReorderFieldOptionsCommand(field.Id, optionIds);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new ReorderFieldOptionsCommand(Guid.CreateVersion7(), [Guid.CreateVersion7()]);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_IncompleteList_ThrowsDomainBusinessRuleException()
    {
        var field = CreateBoardField(FieldType.Select);
        field.AddOption("Option 1", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        field.AddOption("Option 2", Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardFields(field);

        var command = new ReorderFieldOptionsCommand(field.Id, [field.Options.First().Id]);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
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

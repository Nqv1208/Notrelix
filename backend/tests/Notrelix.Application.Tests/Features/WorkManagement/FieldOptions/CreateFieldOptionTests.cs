using Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.CreateFieldOption;

namespace Notrelix.Application.Tests.Features.WorkManagement.FieldOptions;

public class CreateFieldOptionTests : WorkManagementHandlerTestBase
{
    private readonly CreateFieldOptionCommandHandler _handler;

    public CreateFieldOptionTests()
    {
        _handler = new CreateFieldOptionCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_SelectField_CreatesOption()
    {
        var field = CreateBoardField(FieldType.Select);
        SetupBoardFields(field);

        var command = new CreateFieldOptionCommand(field.Id, "Option 1", "#FF0000", 1.0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(field.Id);
    }

    [Fact]
    public async Task Handle_StatusField_CreatesOption()
    {
        var field = CreateBoardField(FieldType.Status);
        SetupBoardFields(field);

        var command = new CreateFieldOptionCommand(field.Id, "In Progress", "#FFFF00", 1.0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TextField_ThrowsDomainBusinessRuleException()
    {
        var field = CreateBoardField(FieldType.Text);
        SetupBoardFields(field);

        var command = new CreateFieldOptionCommand(field.Id, "Option 1", "#FF0000", 1.0);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new CreateFieldOptionCommand(Guid.CreateVersion7(), "Option 1", "#FF0000", 1.0);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateOptionName_ThrowsDomainBusinessRuleException()
    {
        var field = CreateBoardField(FieldType.Select);
        field.AddOption("Option 1", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(field);

        var command = new CreateFieldOptionCommand(field.Id, "Option 1", "#00FF00", 1.0);

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

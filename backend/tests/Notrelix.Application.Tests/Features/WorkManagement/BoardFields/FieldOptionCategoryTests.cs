using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

/// <summary>
/// The option commands carry an optional public work-status category
/// ("todo" | "in_progress" | "review" | "done") for options of a Status field.
/// </summary>
public class FieldOptionCategoryTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();

    public FieldOptionCategoryTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(TestUserId);
    }

    private BoardField StatusField(Guid boardId, bool seeded = true)
    {
        var field = BoardField.Create(TestAccountId, TestWorkspaceId, boardId, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), TestUserId, TestNow);
        if (seeded) field.AddDefaultStatusOptions(TestUserId, TestNow);
        return field;
    }

    private AddFieldOptionCommandHandler AddHandler() =>
        new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object, Placement);

    private UpdateFieldOptionCommandHandler UpdateHandler() =>
        new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);

    [Theory]
    [InlineData("todo", WorkStatusCategory.Todo)]
    [InlineData("in_progress", WorkStatusCategory.InProgress)]
    [InlineData("review", WorkStatusCategory.Review)]
    [InlineData("DONE", WorkStatusCategory.Done)]
    public async Task AddOption_ToStatusField_UsesTheRequestedCategory(string category, WorkStatusCategory expected)
    {
        var boardId = Guid.CreateVersion7();
        var field = StatusField(boardId);
        SetupBoardFields(field);

        var result = await AddHandler().Handle(new AddFieldOptionCommand(boardId, field.Id, "Mới", "#808080", field.Version, Category: category), CancellationToken.None);

        field.Options.Single(o => o.Id == result.Data).Category.Should().Be(expected);
    }

    [Fact]
    public async Task AddOption_ToStatusField_WithoutCategory_DefaultsToInProgress()
    {
        var boardId = Guid.CreateVersion7();
        var field = StatusField(boardId);
        SetupBoardFields(field);

        var result = await AddHandler().Handle(new AddFieldOptionCommand(boardId, field.Id, "Legacy client", "#808080", field.Version), CancellationToken.None);

        field.Options.Single(o => o.Id == result.Data).Category.Should().Be(WorkStatusCategory.InProgress);
    }

    [Fact]
    public async Task AddOption_ToSelectField_WithCategory_IsRejectedByTheDomain()
    {
        var boardId = Guid.CreateVersion7();
        var field = BoardField.Create(TestAccountId, TestWorkspaceId, boardId, "Priority", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a2"), TestUserId, TestNow);
        SetupBoardFields(field);

        var act = () => AddHandler().Handle(new AddFieldOptionCommand(boardId, field.Id, "High", "#808080", field.Version, Category: "done"), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionCategoryNotAllowed);
        field.Options.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOption_ChangesTheCategory_AndBumpsTheFieldVersion()
    {
        var boardId = Guid.CreateVersion7();
        var field = StatusField(boardId);
        SetupBoardFields(field);
        var review = field.Options.Single(o => o.Category == WorkStatusCategory.Review);
        var version = field.Version;

        var result = await UpdateHandler().Handle(
            new UpdateFieldOptionCommand(boardId, field.Id, review.Id, review.Name, review.Color.Value, version, Category: "in_progress"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        review.Category.Should().Be(WorkStatusCategory.InProgress);
        field.Version.Should().Be(version + 1);
    }

    [Fact]
    public async Task UpdateOption_WithoutCategory_KeepsIt()
    {
        var boardId = Guid.CreateVersion7();
        var field = StatusField(boardId);
        SetupBoardFields(field);
        var done = field.Options.Single(o => o.Category == WorkStatusCategory.Done);

        await UpdateHandler().Handle(
            new UpdateFieldOptionCommand(boardId, field.Id, done.Id, "Xong", done.Color.Value, field.Version),
            CancellationToken.None);

        done.Name.Should().Be("Xong");
        done.Category.Should().Be(WorkStatusCategory.Done);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("todo", true)]
    [InlineData("in_progress", true)]
    [InlineData("review", true)]
    [InlineData("done", true)]
    [InlineData("InProgress", false)]
    [InlineData("1", false)]
    [InlineData("", false)]
    [InlineData("closed", false)]
    public void Validators_AcceptOnlyThePublicCategoryNames(string? category, bool valid)
    {
        var add = new AddFieldOptionCommandValidator().Validate(
            new AddFieldOptionCommand(Guid.NewGuid(), Guid.NewGuid(), "A", "#808080", 1, Category: category));
        var update = new UpdateFieldOptionCommandValidator().Validate(
            new UpdateFieldOptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "A", "#808080", 1, Category: category));

        add.IsValid.Should().Be(valid);
        update.IsValid.Should().Be(valid);
    }
}

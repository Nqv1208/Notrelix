using Notrelix.Domain.Common;
using Notrelix.Domain.WorkManagement.Fields.Events;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

/// <summary>
/// UC-WM-C07: the add, update (rename and recolour) and move option commands of a Select field. Each success
/// bumps the field version once and raises one event; a refusal and a semantic no-op change nothing.
/// </summary>
public class FieldOptionCommandsTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Guid _boardId = Guid.CreateVersion7();
    private readonly BoardField _field;
    private readonly FieldOption _high;
    private readonly FieldOption _low;

    public FieldOptionCommandsTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _field = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, "Risk", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _high = _field.AddOption("High", Color.Create("#E5484D"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _low = _field.AddOption("Low", Color.Create("#30A46C"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        ((IHasDomainEvents)_field).ClearDomainEvents();
        SetupBoardFields(_field);
        DbContextMock.Setup(c => c.FieldOptions).Returns(CreateAsyncDbSet(_field.Options.ToList()));
    }

    private AddFieldOptionCommandHandler Add() => new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object, Placement);
    private UpdateFieldOptionCommandHandler Update() => new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);
    private MoveFieldOptionCommandHandler Move() => new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object, Placement);

    [Fact]
    public async Task Add_AppendsTheOptionLastBumpsTheVersionOnceAndRaisesOneEvent()
    {
        var before = _field.Version;

        var result = await Add().Handle(new AddFieldOptionCommand(_boardId, _field.Id, "  Medium  ", "#F5A623", before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Version.Should().Be(before + 1);
        _field.Options.OrderBy(o => o.Position.Value, StringComparer.Ordinal).Select(o => o.Name).Should().Equal("High", "Low", "Medium");
        _field.DomainEvents.Should().ContainSingle(e => e is FieldOptionAddedDomainEvent);
    }

    [Fact]
    public async Task Add_PlacedAfterAnOption_LandsBetweenItAndTheNext()
    {
        var result = await Add().Handle(
            new AddFieldOptionCommand(_boardId, _field.Id, "Medium", "#F5A623", _field.Version, PreviousSiblingId: _high.Id, NextSiblingId: _low.Id),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _field.Options.OrderBy(o => o.Position.Value, StringComparer.Ordinal).Select(o => o.Name).Should().Equal("High", "Medium", "Low");
    }

    [Fact]
    public async Task Add_ADuplicateNameIgnoringCase_IsRefusedAndChangesNothing()
    {
        var before = _field.Version;

        var act = () => Add().Handle(new AddFieldOptionCommand(_boardId, _field.Id, "high", "#808080", before), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName);
        _field.Options.Should().HaveCount(2);
        _field.Version.Should().Be(before);
        _field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_ToAFieldOfAnotherBoard_IsNotFound()
    {
        var act = () => Add().Handle(new AddFieldOptionCommand(Guid.CreateVersion7(), _field.Id, "Medium", "#808080", _field.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _field.Options.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_RenamesAndRecoloursTheOptionBumpsTheVersionOnceAndRaisesOneEvent()
    {
        var before = _field.Version;

        var result = await Update().Handle(
            new UpdateFieldOptionCommand(_boardId, _field.Id, _high.Id, "  Critical ", "#AA0000", before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _high.Name.Should().Be("Critical");
        _high.Color.Value.Should().Be("#AA0000");
        _field.Version.Should().Be(before + 1);
        _field.DomainEvents.Should().ContainSingle(e => e is FieldOptionUpdatedDomainEvent);
    }

    [Fact]
    public async Task Update_WithTheSameNameAndColour_IsANoOpWithoutVersionBumpOrEvent()
    {
        var before = _field.Version;

        var result = await Update().Handle(
            new UpdateFieldOptionCommand(_boardId, _field.Id, _high.Id, "High", _high.Color.Value, before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _field.Version.Should().Be(before);
        _field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ToTheNameOfAnotherOption_IsRefusedAndKeepsTheOption()
    {
        var act = () => Update().Handle(
            new UpdateFieldOptionCommand(_boardId, _field.Id, _high.Id, "LOW", "#E5484D", _field.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName);
        _high.Name.Should().Be("High");
    }

    [Fact]
    public async Task Update_AnUnknownOption_IsRefused()
    {
        var act = () => Update().Handle(
            new UpdateFieldOptionCommand(_boardId, _field.Id, Guid.CreateVersion7(), "Ghost", "#808080", _field.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound);
    }

    [Fact]
    public async Task Move_ToTheFrontReordersTheOptionsBumpsTheVersionOnceAndRaisesOneEvent()
    {
        var before = _field.Version;

        var result = await Move().Handle(
            new MoveFieldOptionCommand(_boardId, _field.Id, _low.Id, before, NextSiblingId: _high.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _field.Options.OrderBy(o => o.Position.Value, StringComparer.Ordinal).Select(o => o.Name).Should().Equal("Low", "High");
        _field.Version.Should().Be(before + 1);
        _field.DomainEvents.Should().ContainSingle(e => e is FieldOptionMovedDomainEvent);
    }

    [Fact]
    public async Task Move_ToThePlaceItAlreadyHolds_IsANoOpWithoutVersionBumpOrEvent()
    {
        var before = _field.Version;

        var result = await Move().Handle(
            new MoveFieldOptionCommand(_boardId, _field.Id, _high.Id, before, NextSiblingId: _low.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _field.Version.Should().Be(before);
        _field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_AnUnknownOption_IsNotFoundAndChangesNothing()
    {
        var before = _field.Version;

        var act = () => Move().Handle(new MoveFieldOptionCommand(_boardId, _field.Id, Guid.CreateVersion7(), before), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _field.Version.Should().Be(before);
    }

    [Fact]
    public void Commands_RequireTheUpdateFieldPermissionAndAnExpectedVersion()
    {
        new AddFieldOptionCommand(_boardId, _field.Id, "A", "#808080", 1).Action.Should().Be(PermissionAction.UpdateField);
        new UpdateFieldOptionCommand(_boardId, _field.Id, _high.Id, "A", "#808080", 1).Action.Should().Be(PermissionAction.UpdateField);
        new MoveFieldOptionCommand(_boardId, _field.Id, _high.Id, 1).Action.Should().Be(PermissionAction.UpdateField);
        new AddFieldOptionCommandValidator()
            .Validate(new AddFieldOptionCommand(_boardId, _field.Id, "A", "#808080", 0)).IsValid.Should().BeFalse();
    }
}

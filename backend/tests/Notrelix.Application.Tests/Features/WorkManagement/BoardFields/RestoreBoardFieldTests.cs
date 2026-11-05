using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class RestoreBoardFieldTests : WorkManagementHandlerTestBase
{
    private static readonly Guid Restorer = Guid.CreateVersion7();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkspaceAccessResolver> _access = new();

    public RestoreBoardFieldTests()
    {
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(Restorer, "Lê Hoàng Phúc", null)]);
        Viewer(guest: false);
    }

    private void Viewer(bool guest) => _access
        .Setup(a => a.ResolveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, TestUserId, true, true, guest));

    private RestoreBoardFieldCommandHandler Handler() =>
        new(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _actors.Object, _access.Object);

    private BoardField CreateBoardField() => BoardField.Create(
        TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), "Test Field", FieldType.Text, FieldSettings.Empty(),
        FractionalIndex.Create("a0"), TestUserId, TestNow);

    private BoardField RestoredBy(Guid by)
    {
        var field = CreateBoardField();
        field.Delete(TestUserId, TestNow);
        field.Restore(by, TestNow);
        SetupBoardFields(field);
        return field;
    }

    [Fact]
    public async Task Handle_ActiveField_NamesWhoRestoredIt()
    {
        var field = RestoredBy(Restorer);

        var result = await Handler().Handle(new RestoreBoardFieldCommand(field.BoardId, field.Id), CancellationToken.None);

        result.Data.Should().Be(new RestoredBoardFieldDto(true, "Lê Hoàng Phúc"));
    }

    [Fact]
    public async Task Handle_ActiveField_ForAGuest_NamesNobody()
    {
        var field = RestoredBy(Restorer);
        Viewer(guest: true);

        var result = await Handler().Handle(new RestoreBoardFieldCommand(field.BoardId, field.Id), CancellationToken.None);

        result.Data.Should().Be(new RestoredBoardFieldDto(true, null));
    }

    [Fact]
    public async Task Handle_ActiveField_WhoseRestorerIsAStranger_NamesNobody()
    {
        var field = RestoredBy(Restorer);
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await Handler().Handle(new RestoreBoardFieldCommand(field.BoardId, field.Id), CancellationToken.None);

        result.Data.Should().Be(new RestoredBoardFieldDto(true, null));
    }

    [Fact]
    public async Task Handle_ActiveField_ChangesNothing()
    {
        var field = RestoredBy(Restorer);
        var version = field.Version;

        await Handler().Handle(new RestoreBoardFieldCommand(field.BoardId, field.Id), CancellationToken.None);

        field.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_DeletedField_RestoresIt_AndNamesNobody()
    {
        var field = CreateBoardField();
        field.Delete(TestUserId, TestNow);
        SetupBoardFields(field);
        DateTimeProviderMock.Setup(d => d.UtcNow).Returns(TestNow.AddDays(1));

        var result = await Handler().Handle(new RestoreBoardFieldCommand(field.BoardId, field.Id), CancellationToken.None);

        field.IsDeleted.Should().BeFalse();
        result.Data.Should().Be(new RestoredBoardFieldDto(false, null));
    }
}

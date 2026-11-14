using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups.Events;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class UpdateBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly UpdateBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public UpdateBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UpdateBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task HandleWithTitle_UpdatesTitle()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, "Updated Title", 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleWithColor_UpdatesColor()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, null, 1, "#FF0000");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardGroupCommand(Guid.CreateVersion7(), "Title", 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithBothTitleAndColor_UpdatesBoth()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, "New Title", 1, "#00FF00");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullTitleAndColor_NoChanges()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, null, 1, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Test Group")]
    [InlineData("  Test Group ")]
    public async Task Handle_SameTitle_IsANoOp_NoEventNoVersionBump(string title)
    {
        // UC-WM-E02: saving the name a group already has changes nothing: no rename event (so no
        // activity entry and no realtime change), and the version the client holds stays valid.
        var group = CreateBoardGroup();
        ((IHasDomainEvents)group).ClearDomainEvents();
        var version = group.Version;
        var updatedAt = group.UpdatedAt;
        SetupBoardGroups(group);

        var result = await _handler.Handle(new UpdateBoardGroupCommand(group.Id, title, version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        group.Title.Should().Be("Test Group");
        group.Version.Should().Be(version);
        group.UpdatedAt.Should().Be(updatedAt);
        group.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NewTitle_RaisesOneRenameEvent_WithTheOldTitle()
    {
        var group = CreateBoardGroup();
        ((IHasDomainEvents)group).ClearDomainEvents();
        var version = group.Version;
        SetupBoardGroups(group);

        await _handler.Handle(new UpdateBoardGroupCommand(group.Id, " QA ", version), CancellationToken.None);

        group.Title.Should().Be("QA");
        group.Version.Should().Be(version + 1);
        group.DomainEvents.OfType<BoardGroupRenamedDomainEvent>().Should().ContainSingle()
            .Which.OldTitle.Should().Be("Test Group");
    }

    private BoardGroup CreateBoardGroup(Guid? id = null)
    {
        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            "Test Group",
            Color.Create("#808080"),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            group.GetType().GetProperty(nameof(BoardGroup.Id))!.SetValue(group, id.Value);
        return group;
    }
}

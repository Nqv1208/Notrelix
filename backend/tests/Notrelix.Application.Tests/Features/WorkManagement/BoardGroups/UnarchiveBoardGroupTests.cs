using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;
using DomainException = Notrelix.Domain.Common.Exceptions.DomainException;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class UnarchiveBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly UnarchiveBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public UnarchiveBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UnarchiveBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ArchivedGroup_UnarchivesGroup()
    {
        var group = CreateBoardGroup();
        group.Archive(TestUserId, TestNow);
        SetupBoardGroups(group);

        var command = new UnarchiveBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeletedGroup_ThrowsDomainException()
    {
        var group = CreateBoardGroup();
        group.Delete(TestUserId, TestNow);
        SetupBoardGroups(group);

        var command = new UnarchiveBoardGroupCommand(group.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new UnarchiveBoardGroupCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ActiveGroup_IsIdempotent()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UnarchiveBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
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

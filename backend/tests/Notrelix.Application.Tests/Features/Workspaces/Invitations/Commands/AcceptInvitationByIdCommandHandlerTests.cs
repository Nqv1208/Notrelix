using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

public class AcceptInvitationByIdCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IInvitationAcceptanceService> _acceptanceServiceMock = new();

    private AcceptInvitationByIdCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, _acceptanceServiceMock.Object);

    [Fact]
    public async Task Handle_WhenInvitationExists_ForwardsItAndRequestContextUserIdToAcceptanceService()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        var dto = new AcceptInvitationResultDto("test-workspace", invitation.WorkspaceId);
        _acceptanceServiceMock.Setup(s => s.AcceptAsync(invitation, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AcceptInvitationResultDto>.Success(dto));
        var sut = CreateSut();

        var result = await sut.Handle(
            new AcceptInvitationByIdCommand(invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(dto);
        _acceptanceServiceMock.Verify(
            s => s.AcceptAsync(invitation, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_ThrowsNotFoundExceptionAndDoesNotCallService()
    {
        SetupInvitations();
        var sut = CreateSut();

        var act = () => sut.Handle(
            new AcceptInvitationByIdCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _acceptanceServiceMock.Verify(
            s => s.AcceptAsync(It.IsAny<WorkspaceInvitation>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private WorkspaceInvitation CreateInvitation()
        => WorkspaceInvitation.Create(
            TestAccountId,
            TestWorkspaceId,
            "test@test.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"),
            1,
            TestUserId,
            TestNow);
}
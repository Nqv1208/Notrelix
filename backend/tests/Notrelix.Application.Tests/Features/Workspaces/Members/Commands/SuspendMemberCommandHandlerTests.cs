using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class SuspendMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private SuspendMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenMemberExists_SuspendsSuccessfully()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember());
        var sut = CreateSut();
        var result = await sut.Handle(new SuspendMemberCommand(TestWorkspaceId, TestUserId), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new SuspendMemberCommand(TestWorkspaceId, TestUserId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

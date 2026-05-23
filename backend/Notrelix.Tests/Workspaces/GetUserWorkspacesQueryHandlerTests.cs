using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Workspaces.Queries;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Tests.Workspaces;

public class GetUserWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_active_workspaces_for_user_with_member_counts()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var ownedWorkspace = Workspace.CreateTeam("Owned Workspace", userId, "Owned by current user");
        ownedWorkspace.UpdateSlug("owned-workspace");
        ownedWorkspace.AddMember(teammateId, WorkspaceRole.Member);

        var joinedWorkspace = Workspace.CreateTeam("Joined Workspace", otherOwnerId, "Current user is a member");
        joinedWorkspace.UpdateSlug("joined-workspace");
        joinedWorkspace.AddMember(userId, WorkspaceRole.Admin);

        var archivedWorkspace = Workspace.CreateTeam("Archived Workspace", userId);
        archivedWorkspace.UpdateSlug("archived-workspace");
        archivedWorkspace.Archive();

        var unrelatedWorkspace = Workspace.CreateTeam("Unrelated Workspace", otherOwnerId);
        unrelatedWorkspace.UpdateSlug("unrelated-workspace");

        context.Workspaces.AddRange(ownedWorkspace, joinedWorkspace, archivedWorkspace, unrelatedWorkspace);
        await context.SaveChangesAsync();

        var handler = new GetUserWorkspacesQueryHandler(context);

        var result = await handler.Handle(new GetUserWorkspacesQuery(userId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Select(workspace => workspace.Slug)
            .Should()
            .BeEquivalentTo(["joined-workspace", "owned-workspace"]);

        result.Data!.Single(workspace => workspace.Slug == "owned-workspace")
            .MemberCount.Should().Be(2);
        result.Data!.Single(workspace => workspace.Slug == "joined-workspace")
            .MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_rejects_empty_user_id()
    {
        await using var context = CreateContext();
        var handler = new GetUserWorkspacesQueryHandler(context);

        var result = await handler.Handle(new GetUserWorkspacesQuery(Guid.Empty), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle("User is not authenticated");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-workspaces-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}

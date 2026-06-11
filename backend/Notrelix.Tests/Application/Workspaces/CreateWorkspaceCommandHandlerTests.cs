using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Application.Tests.Auth; // Utilizes AuthTestDbContextFactory

namespace Notrelix.Application.Tests.Workspaces;

public class CreateWorkspaceCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;

    public CreateWorkspaceCommandHandlerTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
    }

    [Fact]
    public async Task Handle_WhenCreatingTeamWorkspace_ShouldSucceedAndAddAsOwner()
    {
        // Arrange
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object);
        var command = new CreateWorkspaceCommand("Awesome Project", "A great software project", false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var workspace = await context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("Awesome Project");
        workspace.Slug.Should().StartWith("awesome-project-");
        workspace.Description.Should().Be("A great software project");
        workspace.IsPersonal.Should().BeFalse();
        workspace.OwnerId.Should().Be(userId);

        workspace.Members.Should().HaveCount(1);
        workspace.Members.First().UserId.Should().Be(userId);
        workspace.Members.First().Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task Handle_WhenCreatingPersonalWorkspace_ShouldSucceedAndUseDefaultPersonalEmoji()
    {
        // Arrange
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object);
        var command = new CreateWorkspaceCommand("My Personal Tasks", null, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("My Personal Tasks");
        workspace.IsPersonal.Should().BeTrue();
        workspace.Icon.Value.Should().Be("📝");
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldSucceedAndAppendUniqueSuffix()
    {
        // Arrange
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        // Seed an existing workspace with the slug "awesome-project"
        var existingWorkspace = Workspace.CreateTeam("Awesome Project", userId);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object);
        var command = new CreateWorkspaceCommand("Awesome Project", "A duplicate project name", false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().NotBe(existingWorkspace.Id);

        var duplicateWorkspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        duplicateWorkspace.Should().NotBeNull();
        duplicateWorkspace!.Name.Should().Be("Awesome Project");
        duplicateWorkspace.Slug.Should().StartWith("awesome-project-");
        duplicateWorkspace.Slug.Length.Should().BeGreaterThan("awesome-project-".Length);
    }
}

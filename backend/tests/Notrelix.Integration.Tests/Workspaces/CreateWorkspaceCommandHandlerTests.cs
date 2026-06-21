using Microsoft.EntityFrameworkCore;
using Moq;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Workspaces;

public class CreateWorkspaceCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IDateTimeProvider> _dateTimeMock;

    public CreateWorkspaceCommandHandlerTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _dateTimeMock = new Mock<IDateTimeProvider>();
        _dateTimeMock.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenCreatingTeamWorkspace_ShouldSucceed()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object, _dateTimeMock.Object);
        var command = new CreateWorkspaceCommand("Awesome Project", "A great software project", false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("Awesome Project");
        workspace.Slug.Should().Be("awesome-project");
        workspace.Description.Should().Be("A great software project");
        workspace.IsPersonal.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCreatingPersonalWorkspace_ShouldSucceed()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object, _dateTimeMock.Object);
        var command = new CreateWorkspaceCommand("My Personal Tasks", null, true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("My Personal Tasks");
        workspace.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldAppendUniqueSuffix()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _dateTimeMock.Setup(d => d.UtcNow).Returns(now);

        var existingWorkspace = Workspace.Create(userId, "Awesome Project", "awesome-project", now);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var handler = new CreateWorkspaceCommandHandler(context, _currentUserMock.Object, _dateTimeMock.Object);
        var command = new CreateWorkspaceCommand("Awesome Project", "A duplicate project name", false);

        var result = await handler.Handle(command, CancellationToken.None);

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

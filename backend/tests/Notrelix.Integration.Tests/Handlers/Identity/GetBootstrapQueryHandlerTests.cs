using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers.Identity;

public class GetBootstrapQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var handler = new GetBootstrapQueryHandler(context, context);

        var result = await handler.Handle(new GetBootstrapQuery(Guid.NewGuid()), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserInfo()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hashedpassword", now);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetBootstrapQueryHandler(context, context);

        var result = await handler.Handle(new GetBootstrapQuery(user.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(user.Id);
        result.Data.User.Email.Should().Be("test@example.com");
        result.Data.User.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_WhenUserHasWorkspaceMembers_ReturnsWorkspaces()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now);
        context.Users.Add(user);

        var workspace = Workspace.Create(Guid.NewGuid(), user.Id, "My Workspace", "my-workspace", now);
        context.Workspaces.Add(workspace);

        var member = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, user.Id, WorkspaceRole.Admin, user.Id, now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        var handler = new GetBootstrapQueryHandler(context, context);

        var result = await handler.Handle(new GetBootstrapQuery(user.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Workspaces.Should().HaveCount(1);
        result.Data.Workspaces[0].Id.Should().Be(workspace.Id);
        result.Data.Workspaces[0].Name.Should().Be("My Workspace");
        result.Data.Workspaces[0].Slug.Should().Be("my-workspace");
        result.Data.Workspaces[0].Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_WhenPersonalWorkspaceExists_ReturnsReadyStatus()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now);
        context.Users.Add(user);

        var personalWorkspace = Workspace.Create(Guid.NewGuid(), user.Id, "Personal", "personal", now, isPersonal: true);
        context.Workspaces.Add(personalWorkspace);
        await context.SaveChangesAsync();

        var handler = new GetBootstrapQueryHandler(context, context);

        var result = await handler.Handle(new GetBootstrapQuery(user.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PersonalWorkspace.Status.Should().Be("ready");
        result.Data.PersonalWorkspace.WorkspaceId.Should().Be(personalWorkspace.Id);
    }

    [Fact]
    public async Task Handle_WhenPersonalWorkspaceMissing_ReturnsPendingStatus()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetBootstrapQueryHandler(context, context);

        var result = await handler.Handle(new GetBootstrapQuery(user.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PersonalWorkspace.Status.Should().Be("pending");
        result.Data.PersonalWorkspace.WorkspaceId.Should().BeNull();
    }
}

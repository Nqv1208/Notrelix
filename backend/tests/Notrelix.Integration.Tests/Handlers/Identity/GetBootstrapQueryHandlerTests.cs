using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.ReadPorts.Identity;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers.Identity;

[Collection("Database")]
public class GetBootstrapQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GetBootstrapQueryHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var currentUser = new FakeCurrentUser { UserId = Guid.NewGuid() };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserInfo()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hashedpassword", now, hasPasswordCredential: true);
        context.Users.Add(user);
        context.AccountMembers.Add(AccountMember.Create(AccountId, user.Id, AccountRole.Owner, user.Id, now));
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { UserId = user.Id };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(user.Id);
        result.Data.User.Email.Should().Be("test@example.com");
        result.Data.User.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_WhenUserHasWorkspaceMembers_ReturnsWorkspaces()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now, hasPasswordCredential: true);
        context.Users.Add(user);
        context.AccountMembers.Add(AccountMember.Create(AccountId, user.Id, AccountRole.Owner, user.Id, now));

        var workspace = Workspace.Create(AccountId, user.Id, "My Workspace", "my-workspace", now);
        context.Workspaces.Add(workspace);

        var member = WorkspaceMember.Create(AccountId, workspace.Id, user.Id, WorkspaceRole.Admin, user.Id, now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { UserId = user.Id };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

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
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now, hasPasswordCredential: true);
        context.Users.Add(user);
        context.AccountMembers.Add(AccountMember.Create(AccountId, user.Id, AccountRole.Owner, user.Id, now));

        var personalWorkspace = Workspace.Create(AccountId, user.Id, "Personal", "personal", now, isPersonal: true);
        context.Workspaces.Add(personalWorkspace);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { UserId = user.Id };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PersonalWorkspace.Status.Should().Be("ready");
        result.Data.PersonalWorkspace.WorkspaceId.Should().Be(personalWorkspace.Id);
    }

    [Fact]
    public async Task Handle_WhenPersonalWorkspaceMissing_ReturnsPendingStatus()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hashedpassword", now, hasPasswordCredential: true);
        context.Users.Add(user);
        context.AccountMembers.Add(AccountMember.Create(AccountId, user.Id, AccountRole.Owner, user.Id, now));
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { UserId = user.Id };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PersonalWorkspace.Status.Should().Be("pending");
        result.Data.PersonalWorkspace.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DoesNotReturnAnotherUsersWorkspaces()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var caller = User.Create("caller@example.com", "Caller", "hashedpassword", now, hasPasswordCredential: true);
        var other = User.Create("other@example.com", "Other", "hashedpassword", now, hasPasswordCredential: true);
        context.Users.Add(caller);
        context.Users.Add(other);
        context.AccountMembers.Add(AccountMember.Create(Guid.NewGuid(), caller.Id, AccountRole.Owner, caller.Id, now));
        context.AccountMembers.Add(AccountMember.Create(Guid.NewGuid(), other.Id, AccountRole.Owner, other.Id, now));

        var otherWorkspace = Workspace.Create(other.Id, other.Id, "Other Workspace", "other-workspace", now);
        context.Workspaces.Add(otherWorkspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(other.Id, otherWorkspace.Id, other.Id, WorkspaceRole.Admin, other.Id, now));
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { UserId = caller.Id };
        var handler = new GetBootstrapQueryHandler(new IdentityBootstrapReadPort(context, context, context), currentUser);

        var result = await handler.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(caller.Id);
        result.Data.Workspaces.Should().BeEmpty(
            "the caller must only see workspaces they are a member of");
    }
}

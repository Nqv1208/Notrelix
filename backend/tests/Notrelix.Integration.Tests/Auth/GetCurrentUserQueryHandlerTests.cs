using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class GetCurrentUserQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GetCurrentUserQueryHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var currentUser = new FakeCurrentRequestContext().AsUser(Guid.NewGuid());

        var handler = new GetCurrentUserQueryHandler(context, currentUser);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnUserDto()
    {
        await using var context = _db.CreateContext();

        var user = User.Create("me@example.com", "Me", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentRequestContext().AsUser(user.Id);
        var handler = new GetCurrentUserQueryHandler(context, currentUser);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(user.Id);
        result.Data!.Email.Should().Be("me@example.com");
        result.Data!.Name.Should().Be("Me");
    }
}

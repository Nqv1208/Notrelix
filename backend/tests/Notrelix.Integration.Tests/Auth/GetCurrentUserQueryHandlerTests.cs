using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;
using Notrelix.Domain.Identity.Users;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Auth;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

        var handler = new GetCurrentUserQueryHandler(context);

        var result = await handler.Handle(new GetCurrentUserQuery
        {
            UserId = Guid.NewGuid()
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnUserDto()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("me@example.com", "Me", "hashed", DateTimeOffset.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetCurrentUserQueryHandler(context);

        var result = await handler.Handle(new GetCurrentUserQuery
        {
            UserId = user.Id
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(user.Id);
        result.Data!.Email.Should().Be("me@example.com");
        result.Data!.Name.Should().Be("Me");
    }
}

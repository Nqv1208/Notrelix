using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class VerifiedEmailBehaviorTests
{
    private sealed record VerifiedRequest : IRequest<string>, IRequireVerifiedEmail;
    private sealed record UnrestrictedRequest : IRequest<string>;

    [Fact]
    public async Task RequestWithoutRequirement_DoesNotReadIdentityUser()
    {
        var lookup = new Mock<IIdentityUserLookupService>(MockBehavior.Strict);
        var behavior = CreateBehavior<UnrestrictedRequest>(Guid.Empty, false, lookup);

        var result = await behavior.Handle(
            new UnrestrictedRequest(),
            _ => Task.FromResult("allowed"),
            CancellationToken.None);

        result.Should().Be("allowed");
        lookup.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequiredButUnauthenticated_ThrowsUnauthorized()
    {
        var lookup = new Mock<IIdentityUserLookupService>(MockBehavior.Strict);
        var behavior = CreateBehavior<VerifiedRequest>(Guid.Empty, false, lookup);

        var act = () => behavior.Handle(
            new VerifiedRequest(),
            _ => Task.FromResult("unexpected"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        lookup.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequiredButEmailUnverified_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var lookup = CreateLookup(userId, emailConfirmed: false);
        var behavior = CreateBehavior<VerifiedRequest>(userId, true, lookup);

        var act = () => behavior.Handle(
            new VerifiedRequest(),
            _ => Task.FromResult("unexpected"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*confirmed*");
    }

    [Fact]
    public async Task RequiredAndEmailVerified_AllowsRequest()
    {
        var userId = Guid.NewGuid();
        var lookup = CreateLookup(userId, emailConfirmed: true);
        var behavior = CreateBehavior<VerifiedRequest>(userId, true, lookup);

        var result = await behavior.Handle(
            new VerifiedRequest(),
            _ => Task.FromResult("allowed"),
            CancellationToken.None);

        result.Should().Be("allowed");
    }

    private static VerifiedEmailBehavior<TRequest, string> CreateBehavior<TRequest>(
        Guid userId,
        bool isAuthenticated,
        Mock<IIdentityUserLookupService> lookup)
        where TRequest : IRequest<string>
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);
        currentUser.SetupGet(user => user.IsAuthenticated).Returns(isAuthenticated);

        return new VerifiedEmailBehavior<TRequest, string>(currentUser.Object, lookup.Object);
    }

    private static Mock<IIdentityUserLookupService> CreateLookup(Guid userId, bool emailConfirmed)
    {
        var lookup = new Mock<IIdentityUserLookupService>();
        lookup.Setup(service => service.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityUserSnapshot(userId, "user@example.test", emailConfirmed, UserStatus.Active));
        return lookup;
    }
}

namespace Notrelix.Application.Tests.Common.Behaviors;

public class ResourceScopeBehaviorTests
{
    [Fact]
    public async Task WhenUserMissing_ShouldThrowUnauthorized_AndNotCallResolver()
    {
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.Setup(t => t.UserId).Returns((Guid?)null);

        var resolver = new Mock<IResourceScopeResolver>();
        var logger = new Mock<ILogger<ResourceScopeBehavior<IResourceScopedRequest, string>>>();
        var behavior = new ResourceScopeBehavior<IResourceScopedRequest, string>(tenant.Object, resolver.Object, logger.Object);

        var request = new TestResourceScopedRequest();
        var next = new Mock<RequestHandlerDelegate<string>>();

        var act = () => behavior.Handle(request, next.Object, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Resource-scoped request requires authenticated user.");

        resolver.Verify(r => r.ResolveAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed record TestResourceScopedRequest : IResourceScopedRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }
}

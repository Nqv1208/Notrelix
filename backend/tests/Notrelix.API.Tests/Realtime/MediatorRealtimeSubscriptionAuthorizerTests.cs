using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// The per-user stream is decided by the connection's own identity, never by the pipeline: a person gets their own stream
/// and any other id is the same denial as a missing scope. The channel scope is delegated to Chat's query.
/// </summary>
public sealed class MediatorRealtimeSubscriptionAuthorizerTests
{
    private static readonly Guid Me = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static MediatorRealtimeSubscriptionAuthorizer Create(Mock<ISender> sender)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IExecutionContextAccessor>().Object);
        services.AddSingleton(new Mock<ICurrentTenantContext>().Object);
        services.AddSingleton(sender.Object);
        var provider = services.BuildServiceProvider();
        return new MediatorRealtimeSubscriptionAuthorizer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MediatorRealtimeSubscriptionAuthorizer>.Instance);
    }

    private static RealtimePrincipal Principal() => new(
        Me, "me@example.com", "Me", "jti-1", null, DateTimeOffset.UtcNow, null, Guid.NewGuid());

    [Fact]
    public async Task A_person_gets_their_own_per_user_stream_without_a_workspace_binding()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var authorizer = Create(sender);

        var result = await authorizer.AuthorizeAsync(Principal(), new RealtimeScope(RealtimeScopeKind.User, Me), CancellationToken.None);

        result.Outcome.Should().Be(RealtimeAuthorizationOutcome.Granted);
        result.Grant.Should().Be(RealtimeSubscriptionGrant.Unbound);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Another_persons_per_user_stream_is_denied_without_asking_the_pipeline()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var authorizer = Create(sender);

        var result = await authorizer.AuthorizeAsync(
            Principal(), new RealtimeScope(RealtimeScopeKind.User, Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(RealtimeAuthorizationOutcome.Denied);
        sender.VerifyNoOtherCalls();
    }
}

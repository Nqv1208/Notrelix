using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Features.WorkManagement.Public.BoardRealtime;
using Notrelix.Application.Features.WorkManagement.Realtime;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// The notifier tolerates a missing collector or board-scope port so that hand-composed test hosts
/// build; the real host must never rely on that. A missing registration there would turn board
/// realtime off silently, so the real composition must hand the notifier both dependencies.
/// </summary>
public sealed class BoardRealtimeNotifierCompositionTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public BoardRealtimeNotifierCompositionTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public void TheRealHost_ResolvesTheNotifier_WithTheEventCollectorAndTheBoardScopePort()
    {
        using var scope = _factory.Services.CreateScope();

        var notifier = scope.ServiceProvider.GetRequiredService<IBoardRealtimeNotifier>();

        notifier.Should().BeOfType<BoardRealtimeNotifier>();
        foreach (var field in new[] { "_scope", "_events" })
        {
            typeof(BoardRealtimeNotifier)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(notifier)
                .Should().NotBeNull($"the real host must register what the notifier needs for {field}");
        }
    }
}

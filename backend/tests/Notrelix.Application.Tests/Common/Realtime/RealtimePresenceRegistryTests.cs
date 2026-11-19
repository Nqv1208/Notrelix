using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Tests.Common.Realtime;

public class RealtimePresenceRegistryTests
{
    private readonly RealtimePresenceRegistry _registry = new();
    private readonly Guid _workspace = Guid.NewGuid();
    private readonly Guid _ana = Guid.NewGuid();

    [Fact]
    public void A_person_is_online_while_one_connection_reports_online()
    {
        var tab1 = Guid.NewGuid();
        var tab2 = Guid.NewGuid();
        _registry.Mark(tab1, _workspace, _ana, online: true);
        _registry.Mark(tab2, _workspace, _ana, online: true);

        _registry.Mark(tab1, _workspace, _ana, online: false);
        _registry.OnlineUserIds(_workspace).Should().Contain(_ana);

        _registry.Drop(tab2);
        _registry.OnlineUserIds(_workspace).Should().BeEmpty();
    }

    [Fact]
    public void Presence_in_another_workspace_does_not_count()
    {
        _registry.Mark(Guid.NewGuid(), Guid.NewGuid(), _ana, online: true);

        _registry.OnlineUserIds(_workspace).Should().BeEmpty();
    }
}

using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Application.Features.Integrations.Connections.Queries.GetIntegrationConnection;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Application.Tests.Features.Integrations;

public class GetIntegrationConnectionTests : IDisposable
{
    private static readonly Guid WorkspaceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid OtherWorkspaceId = Guid.Parse("99999999-2222-3333-4444-555555555555");
    private static readonly Guid AccountId = Guid.Parse("66666666-7777-8888-9999-000000000000");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);
    private const string SecretReference = "opaque-secret-reference-7f3a";

    private readonly TestIntegrationDbContext _context;
    private readonly GetIntegrationConnectionQueryHandler _handler;

    public GetIntegrationConnectionTests()
    {
        var options = new DbContextOptionsBuilder<TestIntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestIntegrationDbContext(options);
        _handler = new GetIntegrationConnectionQueryHandler(_context);
    }

    public void Dispose() => _context.Dispose();

    private IntegrationConnection AddWebhook(Guid workspaceId)
    {
        var connection = IntegrationConnection.Create(
            AccountId, workspaceId, IntegrationProvider.Webhook, UserId, Now, targetUrl: "https://hooks.example.test/inbox");
        connection.RotateSecret("1", SecretRef.Create(SecretReference), UserId, Now);
        _context.IntegrationConnections.Add(connection);
        _context.IntegrationScopes.Add(IntegrationScope.Create(connection.Id, "work:write"));
        _context.IntegrationScopes.Add(IntegrationScope.Create(connection.Id, "calendar:read"));
        _context.SaveChanges();
        return connection;
    }

    [Fact]
    public async Task Read_ReturnsTheConnection_WithItsScopesInOrder()
    {
        var connection = AddWebhook(WorkspaceId);

        var result = await _handler.Handle(new GetIntegrationConnectionQuery(WorkspaceId, connection.Id), default);

        result.Succeeded.Should().BeTrue();
        result.Data!.Connection.Id.Should().Be(connection.Id);
        result.Data.Connection.Provider.Should().Be("webhook");
        result.Data.Connection.Status.Should().Be("active");
        result.Data.Connection.TargetUrl.Should().Be("https://hooks.example.test/inbox");
        result.Data.Scopes.Should().Equal("calendar:read", "work:write");
    }

    [Fact]
    public async Task Read_OfAnotherWorkspace_IsNotFound()
    {
        var connection = AddWebhook(OtherWorkspaceId);

        var act = () => _handler.Handle(new GetIntegrationConnectionQuery(WorkspaceId, connection.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Read_OfAMadeUpId_IsNotFound()
    {
        var act = () => _handler.Handle(new GetIntegrationConnectionQuery(WorkspaceId, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Read_OfARevokedConnection_IsNotFound()
    {
        var connection = AddWebhook(WorkspaceId);
        connection.Disconnect(UserId, Now.AddMinutes(1));
        _context.SaveChanges();

        var act = () => _handler.Handle(new GetIntegrationConnectionQuery(WorkspaceId, connection.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void DetailDto_HasNoSecretTokenDigestOrReferenceMember()
    {
        var members = typeof(IntegrationConnectionDetailDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Concat(typeof(IntegrationConnectionDto).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))
            .ToList();

        members.Should().NotContain(name =>
            name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Digest", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Hash", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Signature", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Reference", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SerializedDetail_NeverContainsTheSecretReference()
    {
        var connection = AddWebhook(WorkspaceId);

        var result = await _handler.Handle(new GetIntegrationConnectionQuery(WorkspaceId, connection.Id), default);
        var json = JsonSerializer.Serialize(result.Data);

        json.Should().NotContain(SecretReference);
        json.Should().NotContainAny("whsec_", "sha256", "secretVersion", "CurrentSecret");
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Integrations.Connections.Commands.RegenerateWebhookSecret;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Integrations;

public class RegenerateWebhookSecretTests : IDisposable
{
    private static readonly Guid WorkspaceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AccountId = Guid.Parse("66666666-7777-8888-9999-000000000000");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    private readonly TestIntegrationDbContext _context;
    private readonly Mock<IIntegrationSecretStore> _secrets = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly FakeCurrentRequestContext _requestContext = new FakeCurrentRequestContext().AsUser(UserId);
    private readonly List<string> _storedSecrets = [];

    public RegenerateWebhookSecretTests()
    {
        var options = new DbContextOptionsBuilder<TestIntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestIntegrationDbContext(options);
        _secrets.Setup(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((secret, _) => _storedSecrets.Add(secret))
            .ReturnsAsync("ref-2");
    }

    public void Dispose() => _context.Dispose();

    private IntegrationConnection AddWebhook(IntegrationConnectionStatus status = IntegrationConnectionStatus.Active)
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Webhook, UserId, Now, targetUrl: "https://hooks.example.test/inbox");
        connection.RotateSecret("1", SecretRef.Create("ref-1"), UserId, Now);
        if (status == IntegrationConnectionStatus.Paused) connection.Pause(UserId, Now);
        if (status == IntegrationConnectionStatus.Error) connection.RecordFailure("boom", Now);
        _context.IntegrationConnections.Add(connection);
        _context.IntegrationSecretVersions.Add(IntegrationSecretVersion.Create(connection.Id, "1", SecretRef.Create("ref-1"), Now));
        _context.SaveChanges();
        return connection;
    }

    private RegenerateWebhookSecretCommandHandler CreateHandler() => new(
        _context,
        _secrets.Object,
        _requestContext,
        FakeDateTimeProvider.WithFixedTime(Now),
        _audit.Object);

    [Fact]
    public async Task Regenerate_ReturnsANewSecret_AndMakesItTheCurrentVersion()
    {
        var connection = AddWebhook();

        var result = await CreateHandler().Handle(
            new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);

        result.Succeeded.Should().BeTrue();
        result.Data!.SigningSecret.Should().StartWith("whsec_");
        result.Data.SigningSecret.Should().Be(_storedSecrets.Single(), "the caller gets exactly the secret that was stored");
        connection.CurrentSecretVersion.Should().Be("2");
        connection.CurrentSecretRef!.Value.Should().Be("ref-2");
    }

    [Fact]
    public async Task OldVersion_IsNoLongerTheSelectedSecret()
    {
        var connection = AddWebhook();

        await CreateHandler().Handle(new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);
        await _context.SaveChangesAsync();

        // The dispatch consumer selects the secret of the current version only.
        var selected = await _context.IntegrationSecretVersions
            .Where(v => v.ConnectionId == connection.Id && v.Version == connection.CurrentSecretVersion)
            .Select(v => v.SecretReference.Value)
            .ToListAsync();
        selected.Should().Equal("ref-2");
    }

    [Fact]
    public async Task AuditRow_NamesTheVersion_AndNeverTheSecret()
    {
        var connection = AddWebhook();

        var result = await CreateHandler().Handle(
            new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);

        _audit.Verify(a => a.RecordAsync(
            WorkspaceId,
            UserId,
            "RegenerateWebhookSecret",
            It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(),
            AuditSeverity.Info,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<AuditChange?>()), Times.Once);
        var recorded = JsonSerializer.Serialize(_audit.Invocations.Single().Arguments.Last());
        recorded.Should().Contain("\"secretVersion\":\"2\"");
        recorded.Should().NotContain(result.Data!.SigningSecret);
        recorded.Should().NotContain("ref-2");
    }

    [Fact]
    public async Task Paused_Connection_CanRegenerate()
    {
        var connection = AddWebhook(IntegrationConnectionStatus.Paused);

        var result = await CreateHandler().Handle(
            new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(IntegrationConnectionStatus.Error)]
    public async Task ErrorConnection_IsRefused_AndNoSecretIsStored(IntegrationConnectionStatus status)
    {
        var connection = AddWebhook(status);

        var result = await CreateHandler().Handle(
            new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.webhook.secret.connection-state");
        _storedSecrets.Should().BeEmpty();
        connection.CurrentSecretVersion.Should().Be("1");
    }

    [Fact]
    public async Task RevokedConnection_IsNotFound()
    {
        var connection = AddWebhook();
        connection.Disconnect(UserId, Now.AddMinutes(1));
        await _context.SaveChangesAsync();

        var act = () => CreateHandler().Handle(
            new RegenerateWebhookSecretCommand(WorkspaceId, connection.Id, connection.Version), default);

        await act.Should().ThrowAsync<NotFoundException>();
        _storedSecrets.Should().BeEmpty();
    }

    [Fact]
    public async Task MadeUpConnectionId_IsNotFound()
    {
        AddWebhook();

        var act = () => CreateHandler().Handle(new RegenerateWebhookSecretCommand(WorkspaceId, Guid.CreateVersion7(), 1), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_RequiresAPositiveExpectedVersion(long expectedVersion)
    {
        var validator = new RegenerateWebhookSecretCommandValidator();

        validator.Validate(new RegenerateWebhookSecretCommand(WorkspaceId, Guid.NewGuid(), expectedVersion)).IsValid.Should().BeFalse();
    }
}

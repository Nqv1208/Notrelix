using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Application.Features.Integrations.Connections.Commands.CompleteIntegrationAuthorization;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Integrations;

/// <summary>
/// UC-INT-C02, UC-INT-C03 and UC-INT-E03 at the handler boundary: the signed state must name this member,
/// this workspace and this provider; a refused exchange creates nothing; a successful exchange stores the
/// tokens as one secret under an opaque reference and never on the connection or in the result.
/// </summary>
public sealed class CompleteIntegrationAuthorizationCommandHandlerTests : IDisposable
{
    private const string GoogleKey = "google-calendar";
    private const string ValidState = "valid-state";
    private const string RedirectUri = "https://api.notrelix.test/api/v1/integrations/google/callback";
    private const string RawAccessToken = "raw-access-token-value";
    private const string RawRefreshToken = "raw-refresh-token-value";

    private static readonly Guid WorkspaceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AccountId = Guid.Parse("66666666-7777-8888-9999-000000000000");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly TestIntegrationDbContext _context;
    private readonly Mock<IIntegrationSecretStore> _secrets = new();
    private readonly Mock<IIntegrationOAuthClient> _client = new();
    private readonly Mock<IIntegrationOAuthStateProtector> _stateProtector = new();
    private readonly Mock<IIntegrationProviderSettings> _settings = new();
    private readonly FakeCurrentRequestContext _requestContext = new FakeCurrentRequestContext().AsUser(UserId);

    public CompleteIntegrationAuthorizationCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestIntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestIntegrationDbContext(options);
        _requestContext.Tenant.SetAccount(AccountId, UserId);

        _stateProtector.Setup(p => p.Unprotect(ValidState))
            .Returns(new IntegrationOAuthState(GoogleKey, WorkspaceId, UserId, RedirectUri));
        _settings.Setup(s => s.IsConfigured(IntegrationProvider.Google)).Returns(true);
        _client.Setup(c => c.ExchangeCodeAsync(IntegrationProvider.Google, "good-code", RedirectUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationOAuthTokens(RawAccessToken, RawRefreshToken, "provider-account-1", ["calendar.readonly"]));
        _secrets.Setup(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("secret-ref-1");
    }

    public void Dispose() => _context.Dispose();

    private CompleteIntegrationAuthorizationCommandHandler CreateHandler() => new(
        _context,
        _secrets.Object,
        _client.Object,
        _stateProtector.Object,
        _settings.Object,
        _requestContext,
        FakeDateTimeProvider.WithFixedTime(Now));

    private static CompleteIntegrationAuthorizationCommand Command(
        string code = "good-code", string state = ValidState, Guid? workspaceId = null, string provider = GoogleKey) =>
        new(workspaceId ?? WorkspaceId, provider, code, state);

    private async Task<Result<IntegrationConnectionDto>> RunAsync(CompleteIntegrationAuthorizationCommand command) =>
        await CreateHandler().Handle(command, CancellationToken.None);

    private void AssertNothingWasExchangedOrStored()
    {
        _client.Verify(c => c.ExchangeCodeAsync(It.IsAny<IntegrationProvider>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _secrets.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.IntegrationConnections.Local.Should().BeEmpty("a refused request creates no connection");
    }

    [Fact]
    public async Task UnreadableState_IsRefusedBeforeTheCodeIsExchanged()
    {
        _stateProtector.Setup(p => p.Unprotect("tampered-state")).Returns((IntegrationOAuthState?)null);

        var result = await RunAsync(Command(state: "tampered-state"));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.invalid-state");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task ExpiredState_IsRefusedBeforeTheCodeIsExchanged()
    {
        // The protector rejects a state past its lifetime by returning null; the handler must not
        // fall through to the exchange when that happens.
        _stateProtector.Setup(p => p.Unprotect("expired-state")).Returns((IntegrationOAuthState?)null);

        var result = await RunAsync(Command(state: "expired-state"));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.invalid-state");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task StateIssuedForAnotherWorkspace_IsRefused()
    {
        var otherWorkspace = Guid.NewGuid();

        var result = await RunAsync(Command(workspaceId: otherWorkspace));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.invalid-state");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task StateIssuedForAnotherMember_IsRefused()
    {
        _requestContext.AsUser(Guid.NewGuid());

        var result = await RunAsync(Command());

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.invalid-state");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task StateIssuedForAnotherProvider_IsRefused()
    {
        var result = await RunAsync(Command(provider: "slack"));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.invalid-state");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task ProviderRefusesTheCode_CreatesNoConnectionAndSaysRefused()
    {
        _client.Setup(c => c.ExchangeCodeAsync(IntegrationProvider.Google, "replayed-code", RedirectUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegrationOAuthTokens?)null);

        var result = await RunAsync(Command(code: "replayed-code"));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.authorization.refused");
        _secrets.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.IntegrationConnections.Local.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderNotConfigured_IsRefusedBeforeTheExchange()
    {
        _settings.Setup(s => s.IsConfigured(IntegrationProvider.Google)).Returns(false);

        var result = await RunAsync(Command());

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "integrations.provider.not-configured");
        AssertNothingWasExchangedOrStored();
    }

    [Fact]
    public async Task ExchangeSucceeds_CreatesAnActiveConnection_AndStoresTheTokensBehindAnOpaqueReference()
    {
        var result = await RunAsync(Command());

        result.Succeeded.Should().BeTrue();
        result.Data!.Status.Should().Be("active");
        result.Data.Provider.Should().Be(GoogleKey);

        var connection = _context.IntegrationConnections.Local.Should().ContainSingle().Subject;
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.WorkspaceId.Should().Be(WorkspaceId);
        connection.AccountId.Should().Be(AccountId);
        connection.CurrentSecretVersion.Should().Be("1");
        connection.CurrentSecretRef.Should().NotBeNull();
        connection.CurrentSecretRef!.Value.Should().Be("secret-ref-1", "the connection holds only the reference returned by the secret store");

        var versions = _context.IntegrationSecretVersions.Local.Should().ContainSingle().Subject;
        versions.ConnectionId.Should().Be(connection.Id);
        versions.SecretReference.Value.Should().Be("secret-ref-1");
    }

    [Fact]
    public async Task ExchangeSucceeds_TokensGoOnlyToTheSecretStore_NotToTheConnectionOrTheResult()
    {
        var result = await RunAsync(Command());

        result.Succeeded.Should().BeTrue();
        _secrets.Verify(s => s.StoreAsync(
            It.Is<string>(json => json.Contains(RawAccessToken) && json.Contains(RawRefreshToken)),
            It.IsAny<CancellationToken>()), Times.Once,
            "the raw tokens are handed to the secret store exactly once");

        var connection = _context.IntegrationConnections.Local.Should().ContainSingle().Subject;
        var connectionJson = JsonSerializer.Serialize(new
        {
            connection.CurrentSecretRef,
            connection.CurrentSecretVersion,
            connection.ProviderAccountId,
        });
        connectionJson.Should().NotContain(RawAccessToken).And.NotContain(RawRefreshToken);

        var resultJson = JsonSerializer.Serialize(result.Data);
        resultJson.Should().NotContain(RawAccessToken).And.NotContain(RawRefreshToken).And.NotContain("secret-ref-1",
            "the response carries the connection view only, never a secret or its reference");
    }

    [Fact]
    public async Task ExpiredConnection_IsReauthorizedInPlace_WithANewSecretVersion()
    {
        var existing = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, UserId, Now, "provider-account-1");
        existing.RotateSecret("1", SecretRef.Create("old-secret-ref"), UserId, Now);
        existing.MarkExpired(UserId, Now);
        _context.IntegrationConnections.Add(existing);
        await _context.SaveChangesAsync();

        var result = await RunAsync(Command());

        result.Succeeded.Should().BeTrue();
        _context.IntegrationConnections.Local.Should().ContainSingle("the same logical connection is reused, not duplicated")
            .Which.Should().BeSameAs(existing);
        existing.Status.Should().Be(IntegrationConnectionStatus.Active);
        existing.CurrentSecretVersion.Should().Be("2");
        existing.CurrentSecretRef!.Value.Should().Be("secret-ref-1");
    }
}

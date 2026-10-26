using Notrelix.Testing.Core;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// The anonymous invitation-link flow over the real HTTP production graph on PostgreSQL: sign-in
/// identification, the invitation preview states, declining by token, and accepting an invitation by
/// creating the account in the same transaction. The API connects as the container's superuser, like
/// every HTTP production-graph test here, so these tests do not exercise the row-level-security
/// policies (anonymous requests have no tenant context; see the open decision in the hand-off).
/// </summary>
[Collection("Cache")]
public sealed class InvitationTokenFlowHttpTests : IAsyncLifetime
{
    private const string InviteeEmail = "invitee@example.com";
    private static readonly string Password = $"Pw-{Guid.NewGuid():N}-9";

    private readonly CacheTestContainer _fixture;
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private Guid _ownerId;
    private Guid _accountId;
    private Guid _workspaceId;
    private string _workspaceSlug = string.Empty;
    private Guid _boardId;

    public InvitationTokenFlowHttpTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        var owner = User.Create("owner@example.com", "Olivia Owner", "hashed", _now, hasPasswordCredential: true);
        owner.ConfirmEmail(owner.Id, _now);
        _ownerId = owner.Id;
        var account = Account.Create("Acme", $"acme-{Guid.NewGuid():N}"[..20], AccountType.Team, owner.Id, _now);
        _accountId = account.Id;
        _workspaceSlug = $"acme-{Guid.NewGuid():N}"[..20];
        var workspace = Workspace.Create(account.Id, owner.Id, "Acme Workspace", _workspaceSlug, _now);
        _workspaceId = workspace.Id;
        var board = Board.Create(account.Id, workspace.Id, owner.Id, "Roadmap", null, _now);
        _boardId = board.Id;

        await using var seed = SystemContext();
        seed.Users.Add(owner);
        seed.Accounts.Add(account);
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext SystemContext()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return _fixture.CreatePostgresOwnerContext(tenant);
    }

    private async Task<string> SeedInvitationAsync(
        Func<InvitationTokenHash, int, WorkspaceInvitation> create,
        Action<WorkspaceInvitation>? mutate = null)
    {
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = create(InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion);
        mutate?.Invoke(invitation);
        await using var seed = SystemContext();
        seed.WorkspaceInvitations.Add(invitation);
        await seed.SaveChangesAsync();
        return issued.RawToken;
    }

    private Task<string> SeedWorkspaceInvitationAsync(
        DateTimeOffset? createdAt = null, Action<WorkspaceInvitation>? mutate = null) =>
        SeedInvitationAsync(
            (hash, version) => WorkspaceInvitation.Create(
                _accountId, _workspaceId, InviteeEmail, WorkspaceRole.Member, hash, version, _ownerId,
                createdAt ?? _now.AddMinutes(-5)),
            mutate);

    private Task<string> SeedBoardInvitationAsync(Guid boardId) =>
        SeedInvitationAsync((hash, version) => WorkspaceInvitation.CreateForResource(
            _accountId, _workspaceId, InviteeEmail,
            new WorkspaceInvitationResourceGrant(WorkspaceInvitationResourceGrant.BoardKind, boardId, "Editor"),
            hash, version, _ownerId, _now.AddMinutes(-5)));

    private async Task SeedRegisteredInviteeAsync()
    {
        var user = User.Create(InviteeEmail, "Existing Invitee", "hashed", _now, hasPasswordCredential: true);
        user.ConfirmEmail(user.Id, _now);
        await using var seed = SystemContext();
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            builder.UseEnvironment("Testing");

            foreach (var (rateLimitKey, rateLimitValue) in RateLimitTestSettings.Generous)
            {
                builder.UseSetting(rateLimitKey, rateLimitValue);
            }

            var keysPath = Path.Combine(Path.GetTempPath(), "notrelix-invitation-http-keys", Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:NotrelixWorkerDb"] = _fixture.PostgresWorkerConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["JwtSettings:SecretKey"] = "invitation-http-test-signing-key-ThisIsNotASecret-0123456789",
                ["JwtSettings:Issuer"] = "https://notrelix.test",
                ["JwtSettings:Audience"] = "notrelix-api",
                ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
                ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["Smtp:Enabled"] = "true",
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "587",
                ["Smtp:FromEmail"] = "graph@notrelix.test",
                ["DataProtection:PersistKeys"] = "true",
                ["DataProtection:KeysPath"] = keysPath,
                ["Messaging:Transport"] = "InMemory",
                ["Billing:Mode"] = "Database",
                ["Storage:Mode"] = "Local",
                ["Security:Csrf:Enabled"] = "true",
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
            };
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        });

    private static async Task<HttpClient> CreateCsrfClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("csrf_token=", StringComparison.Ordinal)).Split(';', 2)[0];
        client.DefaultRequestHeaders.Add("X-CSRF-Token", token);
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    // ── identify ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Identify_KnownAndUnknownEmail_GiveTheSameAnswer()
    {
        await SeedRegisteredInviteeAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var known = await client.PostAsJsonAsync("/api/v1/auth/identify", new { email = InviteeEmail });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/identify", new { email = "nobody@example.com" });

        known.StatusCode.Should().Be(HttpStatusCode.OK);
        unknown.StatusCode.Should().Be(HttpStatusCode.OK);
        var knownBody = await known.Content.ReadAsStringAsync();
        knownBody.Should().Be(await unknown.Content.ReadAsStringAsync());
        JsonDocument.Parse(knownBody).RootElement.GetProperty("method").GetString().Should().Be("password");
    }

    // ── preview ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Preview_ValidInvitation_ShowsTheInviteeEmailAndWhetherItHasAnAccount()
    {
        var token = await SeedBoardInvitationAsync(_boardId);
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await BodyAsync(response);
        body.GetProperty("state").GetString().Should().Be("valid");
        body.GetProperty("inviterName").GetString().Should().Be("Olivia Owner");
        body.GetProperty("workspaceName").GetString().Should().Be("Acme Workspace");
        body.GetProperty("resourceKind").GetString().Should().Be("work-management.board");
        body.GetProperty("resourceTitle").GetString().Should().Be("Roadmap");
        body.GetProperty("email").GetString().Should().Be(InviteeEmail);
        body.GetProperty("hasAccount").GetBoolean().Should().BeFalse();
        body.TryGetProperty("id", out _).Should().BeFalse("the invitation id is not exposed");
    }

    [Fact]
    public async Task Preview_ValidInvitationForAnExistingAccount_SaysSo()
    {
        await SeedRegisteredInviteeAsync();
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var body = await BodyAsync(await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token }));

        body.GetProperty("hasAccount").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Preview_ExpiredAndUsedInvitations_WithholdTheEmail()
    {
        var expired = await SeedWorkspaceInvitationAsync(createdAt: _now.AddDays(-60));
        var declined = await SeedWorkspaceInvitationAsync(mutate: i => i.Decline(null, _now));
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var expiredBody = await BodyAsync(await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token = expired }));
        var usedBody = await BodyAsync(await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token = declined }));

        expiredBody.GetProperty("state").GetString().Should().Be("expired");
        usedBody.GetProperty("state").GetString().Should().Be("used");
        foreach (var body in new[] { expiredBody, usedBody })
        {
            body.GetProperty("email").ValueKind.Should().Be(JsonValueKind.Null);
            body.GetProperty("hasAccount").GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task Preview_UnknownAndMalformedTokens_AreBothNotFound()
    {
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);
        var unknown = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation).RawToken;

        (await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token = unknown }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/api/v1/invitations/preview", new { token = "not-a-token" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── decline by token ──────────────────────────────────────────────────

    [Fact]
    public async Task Decline_ByToken_Is204Twice_AndTheInvitationStaysDeclined()
    {
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        (await client.PostAsJsonAsync("/api/v1/invitations/decline", new { token })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/v1/invitations/decline", new { token })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        await using var probe = SystemContext();
        (await probe.WorkspaceInvitations.SingleAsync()).Status.Should().Be(WorkspaceInvitationStatus.Declined);
    }

    [Fact]
    public async Task Decline_AnAcceptedInvitation_IsRejected_AndUnknownTokenIsNotFound()
    {
        var accepted = await SeedWorkspaceInvitationAsync(mutate: i => i.Accept(Guid.NewGuid(), _now));
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        (await client.PostAsJsonAsync("/api/v1/invitations/decline", new { token = accepted })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        var unknown = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation).RawToken;
        (await client.PostAsJsonAsync("/api/v1/invitations/decline", new { token = unknown })).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    // ── accept with registration ──────────────────────────────────────────

    [Fact]
    public async Task AcceptWithRegistration_CreatesTheIdentityAccountMembershipAndGuestEntry_AndSignsIn()
    {
        var token = await SeedBoardInvitationAsync(_boardId);
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy Invitee", password = Password });

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        var body = JsonDocument.Parse(text).RootElement;
        body.GetProperty("workspaceSlug").GetString().Should().Be(_workspaceSlug);
        body.GetProperty("workspaceId").GetGuid().Should().Be(_workspaceId);
        body.GetProperty("resourceKind").GetString().Should().Be("work-management.board");
        body.GetProperty("resourceId").GetGuid().Should().Be(_boardId);
        body.GetProperty("alreadyAccepted").GetBoolean().Should().BeFalse();
        body.GetProperty("user").GetProperty("email").GetString().Should().Be(InviteeEmail);
        text.Should().NotContain("accessToken", "tokens travel only as cookies");
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("refreshToken=", StringComparison.Ordinal));

        await using var probe = SystemContext();
        var user = await probe.Users.SingleAsync(u => u.NormalizedEmail == InviteeEmail);
        user.EmailConfirmed.Should().BeTrue();
        (await probe.AccountMembers.CountAsync(m => m.UserId == user.Id)).Should().BeGreaterThan(0, "a personal account");
        (await probe.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == _workspaceId && m.UserId == user.Id))
            .Role.Should().Be(WorkspaceRole.Guest);
        (await probe.ResourcePermissions.SingleAsync(p => p.ResourceId == _boardId)).SubjectId.Should().Be(user.Id);
        (await probe.WorkspaceInvitations.SingleAsync()).Status.Should().Be(WorkspaceInvitationStatus.Accepted);
    }

    [Fact]
    public async Task AcceptWithRegistration_SecondPress_Is200AlreadyAccepted_WithoutASession_OrASecondIdentity()
    {
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);
        var payload = new { token, name = "Ivy Invitee", password = Password };
        (await client.PostAsJsonAsync("/api/v1/invitations/accept-with-registration", payload))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/v1/invitations/accept-with-registration", payload);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await BodyAsync(second);
        body.GetProperty("alreadyAccepted").GetBoolean().Should().BeTrue();
        body.GetProperty("workspaceId").GetGuid().Should().Be(_workspaceId);
        second.Headers.Contains("Set-Cookie").Should().BeFalse("a repeat press never signs anyone in");
        await using var probe = SystemContext();
        (await probe.Users.CountAsync(u => u.NormalizedEmail == InviteeEmail)).Should().Be(1);
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == _workspaceId && m.Role == WorkspaceRole.Guest
            || m.WorkspaceId == _workspaceId && m.Role == WorkspaceRole.Member)).Should().Be(1);
    }

    [Fact]
    public async Task AcceptWithRegistration_ForAnExistingEmail_Is409_AndCreatesNothing()
    {
        await SeedRegisteredInviteeAsync();
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy", password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        await using var probe = SystemContext();
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == _workspaceId)).Should().Be(0);
        (await probe.WorkspaceInvitations.SingleAsync()).Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AcceptWithRegistration_WhenAcceptanceFailsAfterTheUserWasCreated_RollsEverythingBack()
    {
        // The invitation names a board that does not exist: the guest entry is refused after the
        // identity and its personal account were already staged in the request transaction.
        var token = await SeedBoardInvitationAsync(Guid.NewGuid());
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy", password = Password });

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        await using var probe = SystemContext();
        (await probe.Users.CountAsync(u => u.NormalizedEmail == InviteeEmail)).Should().Be(0);
        (await probe.AccountMembers.CountAsync()).Should().Be(0);
        (await probe.WorkspaceMembers.CountAsync()).Should().Be(0);
        (await probe.WorkspaceInvitations.SingleAsync()).Status.Should().Be(WorkspaceInvitationStatus.Pending,
            "a rolled-back accept leaves the link usable");
    }

    [Fact]
    public async Task AcceptWithRegistration_WithoutTheCsrfPair_Is403()
    {
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy", password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptWithRegistration_WithAShortPassword_Is400WithAPasswordError_AndCreatesNothing()
    {
        var token = await SeedWorkspaceInvitationAsync();
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await BodyAsync(response);
        body.GetProperty("errorCode").GetString().Should().Be("validation.failed");
        body.GetProperty("errors").EnumerateObject().Select(e => e.Name.ToLowerInvariant()).Should().Contain("password");
        await using var probe = SystemContext();
        (await probe.Users.CountAsync(u => u.NormalizedEmail == InviteeEmail)).Should().Be(0);
    }

    [Fact]
    public async Task AcceptWithRegistration_ExpiredInvitation_IsRejected_AndCreatesNothing()
    {
        var token = await SeedWorkspaceInvitationAsync(createdAt: _now.AddDays(-60));
        using var factory = CreateFactory();
        using var client = await CreateCsrfClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/invitations/accept-with-registration", new { token, name = "Ivy", password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await using var probe = SystemContext();
        (await probe.Users.CountAsync(u => u.NormalizedEmail == InviteeEmail)).Should().Be(0);
    }
}

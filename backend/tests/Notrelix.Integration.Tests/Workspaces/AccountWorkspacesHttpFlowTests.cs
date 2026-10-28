using Notrelix.Testing.Core;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The account workspace list, default workspace, leave and profile photo over the real HTTP
/// production graph (authentication, request pipeline, EF tenant filters, PostgreSQL). It proves the
/// global-scope reads see the caller's memberships through the real request tenant shape and that the
/// routes honour the caller-is-the-subject rule.
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class AccountWorkspacesHttpFlowTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly CacheTestContainer _fixture;
    private readonly Guid _accountId = Guid.NewGuid();
    private User _alice = null!;
    private User _bob = null!;
    private Guid _workspace;

    public AccountWorkspacesHttpFlowTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);
        var now = DateTimeOffset.UtcNow;
        _alice = User.Create("alice-account@example.com", "Alice", hash, now, hasPasswordCredential: true);
        _bob = User.Create("bob-account@example.com", "Bob", hash, now, hasPasswordCredential: true);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _fixture.CreatePostgresOwnerContext(tenant);
        context.Users.AddRange(_alice, _bob);
        await context.SaveChangesAsync();

        var request = new Mock<ICurrentRequestContext>();
        request.Setup(r => r.UserId).Returns(_alice.Id);
        request.Setup(r => r.RequireAccountId()).Returns(_accountId);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(now);
        var projection = new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
        var created = await new CreateWorkspaceCommandHandler(context, request.Object, clock.Object, projection)
            .Handle(new CreateWorkspaceCommand("Acme", null, false), default);
        await context.SaveChangesAsync();
        _workspace = created.Data;
        await new AddMemberCommandHandler(context, request.Object, clock.Object, projection)
            .Handle(new AddMemberCommand(_workspace, _bob.Id, WorkspaceRole.Member), default);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            builder.UseEnvironment("Testing");

            foreach (var (rateLimitKey, rateLimitValue) in RateLimitTestSettings.Generous)
            {
                builder.UseSetting(rateLimitKey, rateLimitValue);
            }

            var storage = Path.Combine(Path.GetTempPath(), "notrelix-account-http-storage", Guid.NewGuid().ToString("N"));
            var keysPath = Path.Combine(Path.GetTempPath(), "notrelix-account-http-keys", Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:NotrelixWorkerDb"] = _fixture.PostgresWorkerConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["JwtSettings:SecretKey"] = "account-http-test-signing-key-ThisIsNotASecret-0123456789",
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
                ["Storage:BasePath"] = storage,
                ["Security:Csrf:Enabled"] = "false",
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
            };
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

    private static async Task<HttpClient> SignedInAsync(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return client;
    }

    [Fact]
    public async Task List_ThroughTheProductionGraph_ReturnsTheCallersWorkspace_AndDefaultAndLeaveFollow()
    {
        using var factory = CreateFactory();
        using var bob = await SignedInAsync(factory, "bob-account@example.com");

        var list = await bob.GetAsync("/api/v1/account/workspaces");
        var body = await list.Content.ReadAsStringAsync();
        list.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var rows = JsonDocument.Parse(body).RootElement;
        rows.GetArrayLength().Should().Be(1, "the caller's active membership is listed through the global-scope request");
        rows[0].GetProperty("name").GetString().Should().Be("Acme");
        rows[0].GetProperty("role").GetString().Should().Be("Member");
        rows[0].GetProperty("memberCount").GetInt32().Should().Be(2);
        rows[0].GetProperty("departmentCount").ValueKind.Should().Be(JsonValueKind.Null);
        rows[0].GetProperty("isDefault").GetBoolean().Should().BeFalse();
        rows[0].GetProperty("canLeave").GetBoolean().Should().BeTrue();

        var put = await bob.PutAsJsonAsync("/api/v1/account/default-workspace", new { workspaceId = _workspace });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent, await put.Content.ReadAsStringAsync());
        var afterDefault = JsonDocument.Parse(await bob.GetStringAsync("/api/v1/account/workspaces")).RootElement;
        afterDefault[0].GetProperty("isDefault").GetBoolean().Should().BeTrue();

        var leave = await bob.DeleteAsync($"/api/v1/account/workspaces/{_workspace}");
        leave.StatusCode.Should().Be(HttpStatusCode.NoContent, await leave.Content.ReadAsStringAsync());
        JsonDocument.Parse(await bob.GetStringAsync("/api/v1/account/workspaces")).RootElement
            .GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Bootstrap_ListsTheCallersActiveWorkspaces_ThroughTheProductionGraph()
    {
        using var factory = CreateFactory();
        using var bob = await SignedInAsync(factory, "bob-account@example.com");

        var response = await bob.GetAsync("/api/v1/auth/bootstrap");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("Acme");
    }

    [Fact]
    public async Task SoleOwnerLeaving_Is409_WithTheStableCode()
    {
        using var factory = CreateFactory();
        using var alice = await SignedInAsync(factory, "alice-account@example.com");

        var response = await alice.DeleteAsync($"/api/v1/account/workspaces/{_workspace}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("workspaces.member.last-owner-cannot-leave");
    }

    [Fact]
    public async Task SettingADefaultForAWorkspaceTheCallerDoesNotBelongTo_IsNotFound()
    {
        using var factory = CreateFactory();
        using var stranger = await SignedInAsync(factory, "bob-account@example.com");
        await stranger.DeleteAsync($"/api/v1/account/workspaces/{_workspace}");

        var response = await stranger.PutAsJsonAsync("/api/v1/account/default-workspace", new { workspaceId = _workspace });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Avatar_UploadServeAndRemove_OverHttp()
    {
        using var factory = CreateFactory();
        using var alice = await SignedInAsync(factory, "alice-account@example.com");

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "me.png");
        var upload = await alice.PutAsync("/api/v1/profile/avatar", form);
        var uploadBody = await upload.Content.ReadAsStringAsync();
        upload.StatusCode.Should().Be(HttpStatusCode.OK, uploadBody);
        var url = JsonDocument.Parse(uploadBody).RootElement.GetProperty("avatarUrl").GetString()!;
        url.Should().StartWith($"/api/v1/users/{_alice.Id:D}/avatar?v=");

        var read = await alice.GetAsync(url);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await read.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
        read.Headers.CacheControl!.Private.Should().BeTrue();
        read.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        var remove = await alice.DeleteAsync("/api/v1/profile/avatar");
        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        (await alice.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CoMember_CanReadAnotherMembersAvatar_AndAStrangerCannot()
    {
        using var factory = CreateFactory();
        using var alice = await SignedInAsync(factory, "alice-account@example.com");
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Png), "file", "me.png");
        (await alice.PutAsync("/api/v1/profile/avatar", form)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var bob = await SignedInAsync(factory, "bob-account@example.com");
        (await bob.GetAsync($"/api/v1/users/{_alice.Id:D}/avatar")).StatusCode.Should().Be(HttpStatusCode.OK);

        await bob.DeleteAsync($"/api/v1/account/workspaces/{_workspace}");
        (await bob.GetAsync($"/api/v1/users/{_alice.Id:D}/avatar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

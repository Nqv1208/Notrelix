using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API;
using Notrelix.Application.Features.WorkManagement.Forms.Services;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Who may switch a form public, over HTTP on the application login through the production host: an Owner or Admin
/// may (when the workspace allows public links), a Member or Guest may not (and is not shown the key); a person of
/// another tenant is told the form does not exist. Switching back to private ends the public link at once, and a
/// switch on while the workspace has public links off is refused.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class FormPublicAccessAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _admin = null!;
    private User _member = null!;
    private User _guest = null!;
    private Form _form = null!;
    private WebApplicationFactory<Program>? _factory;

    public FormPublicAccessAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-form-switch@example.com", "Afs", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-form-switch@example.com", "Bfs", "hash");
        _admin = await AppRoleMembers.AddAsync(_fixture, _a, "FsAdmin", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");
        // The Admin's access grant is projected the way production does, so the engine sees a workspace admin.
        var system0 = new FakeCurrentTenantContext();
        system0.SetSystem();
        await using (var grant = _fixture.CreateOwnerContext(system0))
        {
            await new Notrelix.Infrastructure.Data.Authz.AccessGrantProjectionService(grant)
                .SyncWorkspaceMemberGrantAsync(_a.AccountId, _a.WorkspaceId, _admin.Id, WorkspaceRole.Admin, Now, CancellationToken.None);
            await grant.SaveChangesAsync();
        }
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "FsMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "FsGuest", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");

        var owner = _a.User.Id;
        _form = Form.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Contact", "contact", owner, Now);
        _form.AddQuestion(FormQuestion.Create(_a.AccountId, _a.WorkspaceId, _form.Id, null, "name", "Name", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), owner, Now);
        _form.Publish(owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Forms.Add(_form);
        await ctx.SaveChangesAsync();
        await SetPublicLinksAsync(true);
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task SetPublicLinksAsync(bool on)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        await ctx.Database.ExecuteSqlRawAsync("UPDATE workspace.workspaces SET settings_allow_public_sharing = {0} WHERE id = {1}", on, _a.WorkspaceId);
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString)
        .WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ICaptchaVerifier>();
            s.AddSingleton<ICaptchaVerifier>(new FakeCaptchaVerifier());
        }));

    private HttpClient As(User? who)
    {
        var client = Factory.CreateClient();
        if (who is null) return client;
        using var scope = Factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private Task<HttpResponseMessage> Switch(User who, bool enabled) =>
        As(who).PutAsJsonAsync($"/api/v1/forms/{_form.Id}/public-access", new { enabled });

    private static async Task<(bool IsPublic, string? Key)> ReadAsync(HttpResponseMessage response)
    {
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return (json["isPublic"]!.GetValue<bool>(), json["publicKey"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public async Task AnOwnerOrAdmin_SwitchesTheFormPublic_AndTheLinkWorksForAnAnonymousVisitor(string who)
    {
        var response = await Switch(who == "Owner" ? _a.User : _admin, true);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var (isPublic, key) = await ReadAsync(response);
        isPublic.Should().BeTrue();
        key.Should().HaveLength(22);
        (await As(null).GetAsync($"/api/v1/public/forms/{key}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task AMemberOrGuest_CannotSwitchAFormPublic_AndLearnsNoKey(string who)
    {
        var user = who == "Member" ? _member : _guest;

        var put = await Switch(user, true);
        var get = await As(user).GetAsync($"/api/v1/forms/{_form.Id}/public-access");

        put.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        get.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        (await ctx.Forms.AsNoTracking().SingleAsync(f => f.Id == _form.Id)).PublicKey.Should().BeNull("the refused switch changed nothing");
    }

    [Fact]
    public async Task AnAnonymousCaller_CannotUseTheSwitch()
    {
        var response = await As(null).PutAsJsonAsync($"/api/v1/forms/{_form.Id}/public-access", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task APersonOfAnotherTenant_IsToldTheFormDoesNotExist()
    {
        (await Switch(_b.User, true)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwitchingBackToPrivate_EndsTheLinkAtOnce_AndSwitchingOnAgainMintsAFreshKey()
    {
        var (_, first) = await ReadAsync(await Switch(_a.User, true));
        (await As(null).GetAsync($"/api/v1/public/forms/{first}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var off = await Switch(_admin, false);
        off.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(off)).Should().Be((false, (string?)null));
        (await As(null).GetAsync($"/api/v1/public/forms/{first}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var (_, second) = await ReadAsync(await Switch(_a.User, true));
        second.Should().NotBe(first);
        (await As(null).GetAsync($"/api/v1/public/forms/{first}")).StatusCode.Should().Be(HttpStatusCode.NotFound, "the old link stays dead");
        (await As(null).GetAsync($"/api/v1/public/forms/{second}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwitchingOnTwice_KeepsTheLinkAlreadyShared()
    {
        var (_, first) = await ReadAsync(await Switch(_a.User, true));
        var (_, again) = await ReadAsync(await Switch(_admin, true));

        again.Should().Be(first);
    }

    [Fact]
    public async Task WhenTheWorkspaceTurnsPublicLinksOff_TheSwitchOnIsRefused_AndAnExistingLinkStopsServing()
    {
        var (_, key) = await ReadAsync(await Switch(_a.User, true));

        await SetPublicLinksAsync(false);

        (await As(null).GetAsync($"/api/v1/public/forms/{key}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var refused = await Switch(_a.User, true);
        ((int)refused.StatusCode).Should().BeInRange(400, 499);
        refused.StatusCode.Should().NotBe(HttpStatusCode.OK);
        (await Switch(_a.User, false)).StatusCode.Should().Be(HttpStatusCode.OK, "switching off is never blocked");
    }

    [Fact]
    public async Task OnlyTheOwnerOrAdmin_ReadsTheKey()
    {
        await Switch(_a.User, true);

        var asAdmin = await As(_admin).GetAsync($"/api/v1/forms/{_form.Id}/public-access");

        asAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(asAdmin)).IsPublic.Should().BeTrue();
    }
}

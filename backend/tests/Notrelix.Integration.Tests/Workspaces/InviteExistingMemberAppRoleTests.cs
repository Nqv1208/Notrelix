using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Npgsql;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Inviting a person who already belongs, over the real host as the application role: an Owner or Admin who invites
/// the address of an Active member (Member or Admin) is answered 409 with the stable code
/// <c>workspaces.invitation.already-member</c> and nothing is stored or sent, whatever the casing or the spaces
/// around the address. An Active Guest is not refused: a Guest's address is never disclosed (see the Guest test).
/// A Suspended or Removed member and a person who belongs only to another workspace are invited like anyone else, and the answer for them is the ordinary success, so the refusal cannot be used to learn whom
/// another workspace holds. A Member is forbidden and an outsider is told the workspace does not exist.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class InviteExistingMemberAppRoleTests : IAsyncLifetime
{
    private const string AlreadyMemberCode = "workspaces.invitation.already-member";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private readonly Dictionary<string, User> _people = new();

    public InviteExistingMemberAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-invite-existing@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-invite-existing@example.com", "Bbb", "hash");
        _people["member"] = await AppRoleMembers.AddAsync(_fixture, _a, "Mem", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _people["admin"] = await AppRoleMembers.AddAsync(_fixture, _a, "Adm", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");
        _people["guest"] = await AppRoleMembers.AddAsync(_fixture, _a, "Gst", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");
        _people["suspended"] = await AppRoleMembers.AddAsync(_fixture, _a, "Sus", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");
        _people["removed"] = await AppRoleMembers.AddAsync(_fixture, _a, "Rem", WorkspaceRole.Member, WorkspaceMemberStatus.Removed, "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static HttpClient ClientFor(WebApplicationFactory<Program> factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(user, null);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
        return client;
    }

    private static Task<HttpResponseMessage> InviteAsync(HttpClient client, Guid workspaceId, string email, string role = "Member")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{workspaceId}/members")
        {
            Content = JsonContent.Create(new { email, role }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return client.SendAsync(request);
    }

    private async Task<long> OwnerCountAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private Task<long> InvitationRowsAsync() =>
        OwnerCountAsync("SELECT count(*) FROM workspace.workspace_invitations WHERE workspace_id = @w", ("w", _a.WorkspaceId));

    private Task<long> DeliveriesAsync() =>
        OwnerCountAsync(
            "SELECT count(*) FROM messaging.outbox_messages WHERE account_id = @a AND message_name ILIKE '%invitation%'",
            ("a", _a.AccountId));

    private async Task AssertNothingChangedAsync(long rows, long deliveries)
    {
        (await InvitationRowsAsync()).Should().Be(rows, "no invitation row is stored");
        (await DeliveriesAsync()).Should().Be(deliveries, "no delivery is staged");
    }

    private WebApplicationFactory<Program> Host() => AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    [Theory]
    [InlineData("member", "exact")]
    [InlineData("member", "upper")]
    [InlineData("member", "padded")]
    [InlineData("admin", "exact")]
    [InlineData("admin", "mixed")]
    public async Task AnOwner_InvitingAnActiveMember_IsRefusedWithTheAlreadyMemberCode_AndNothingIsStoredOrSent(string who, string spelling)
    {
        using var factory = Host();
        using var owner = ClientFor(factory, _a.User);
        var email = _people[who].NormalizedEmail;
        var spelled = spelling switch
        {
            "upper" => email.ToUpperInvariant(),
            "mixed" => string.Concat(email.Select((ch, i) => i % 2 == 0 ? char.ToUpperInvariant(ch) : ch)),
            "padded" => $"  {email}  ",
            _ => email,
        };
        var rows = await InvitationRowsAsync();
        var deliveries = await DeliveriesAsync();

        var response = await InviteAsync(owner, _a.WorkspaceId, spelled);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, body);
        body.Should().Contain(AlreadyMemberCode);
        await AssertNothingChangedAsync(rows, deliveries);
    }

    [Theory]
    [InlineData("member")]
    [InlineData("admin")]
    public async Task AnAdmin_InvitingAnActiveMember_IsRefusedTheSameWay(string who)
    {
        using var factory = Host();
        using var admin = ClientFor(factory, _people["admin"]);
        var rows = await InvitationRowsAsync();
        var deliveries = await DeliveriesAsync();

        var response = await InviteAsync(admin, _a.WorkspaceId, _people[who].NormalizedEmail);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, body);
        body.Should().Contain(AlreadyMemberCode);
        await AssertNothingChangedAsync(rows, deliveries);
    }

    [Fact]
    public async Task AnActiveGuest_IsNotRefused_BecauseAGuestsAddressIsNeverDisclosed_AndTheAnswerIsTheOrdinarySuccess()
    {
        // Open decision: refusing a Guest's address would tell an Owner or Admin that the address is a Guest here,
        // which the member-email rule (a Guest's address is never shown) does not allow today. This pins the current
        // answer, and that it is indistinguishable from an unknown address.
        using var factory = Host();
        using var owner = ClientFor(factory, _a.User);
        var rows = await InvitationRowsAsync();
        var deliveries = await DeliveriesAsync();

        var guest = await InviteAsync(owner, _a.WorkspaceId, _people["guest"].NormalizedEmail);
        var unknown = await InviteAsync(owner, _a.WorkspaceId, $"nobody-{Guid.NewGuid():N}@example.com");

        guest.StatusCode.Should().Be(HttpStatusCode.Created, await guest.Content.ReadAsStringAsync());
        unknown.StatusCode.Should().Be(guest.StatusCode);
        (await guest.Content.ReadAsStringAsync()).Should().NotContain(AlreadyMemberCode);
        await AssertNothingChangedAsync(rows + 2, deliveries + 2);
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("removed")]
    public async Task ASuspendedOrRemovedMember_IsInvitedAgain_OneRowAndOneDelivery(string who)
    {
        using var factory = Host();
        using var owner = ClientFor(factory, _a.User);
        var rows = await InvitationRowsAsync();
        var deliveries = await DeliveriesAsync();

        var response = await InviteAsync(owner, _a.WorkspaceId, _people[who].NormalizedEmail);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        await AssertNothingChangedAsync(rows + 1, deliveries + 1);
    }

    [Fact]
    public async Task APersonOfAnotherWorkspaceOnly_IsAnsweredLikeAnUnknownAddress_SoTheRefusalLeaksNothing()
    {
        using var factory = Host();
        using var owner = ClientFor(factory, _a.User);

        var other = await InviteAsync(owner, _a.WorkspaceId, _b.User.NormalizedEmail);
        var unknown = await InviteAsync(owner, _a.WorkspaceId, $"nobody-{Guid.NewGuid():N}@example.com");

        other.StatusCode.Should().Be(HttpStatusCode.Created, await other.Content.ReadAsStringAsync());
        unknown.StatusCode.Should().Be(other.StatusCode);
        (await other.Content.ReadAsStringAsync()).Should().NotContain(AlreadyMemberCode);
    }

    [Fact]
    public async Task AMemberIsForbidden_AnOutsiderIsToldTheWorkspaceDoesNotExist_AndNothingIsStoredOrSent()
    {
        using var factory = Host();
        using var member = ClientFor(factory, _people["member"]);
        using var outsider = ClientFor(factory, _b.User);
        var rows = await InvitationRowsAsync();
        var deliveries = await DeliveriesAsync();

        // The address belongs to an active member, so a leak would show as the 409 instead of the caller's own refusal.
        var asMember = await InviteAsync(member, _a.WorkspaceId, _people["admin"].NormalizedEmail);
        var asOutsider = await InviteAsync(outsider, _a.WorkspaceId, _people["admin"].NormalizedEmail);

        asMember.StatusCode.Should().Be(HttpStatusCode.Forbidden, await asMember.Content.ReadAsStringAsync());
        asOutsider.StatusCode.Should().Be(HttpStatusCode.NotFound, await asOutsider.Content.ReadAsStringAsync());
        (await asMember.Content.ReadAsStringAsync()).Should().NotContain(AlreadyMemberCode);
        (await asOutsider.Content.ReadAsStringAsync()).Should().NotContain(AlreadyMemberCode);
        await AssertNothingChangedAsync(rows, deliveries);
    }
}

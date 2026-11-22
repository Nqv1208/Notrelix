using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// An invitee whose sign-in email was never confirmed (the verification mail was never delivered) accepts by the
/// emailed link, over the real host as the application role. Opening that link proves control of the mailbox, so
/// a valid token for exactly the signed-in address confirms the email and admits the person. A token for another
/// address, a revoked or expired one, and the in-app list path (no token) confirm nothing.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AcceptInvitationUnconfirmedEmailAppRoleTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;

    public AcceptInvitationUnconfirmedEmailAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-accept-unconfirmed@example.com", "Aaa", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(User User, string Token, Guid InvitationId)> InviteAsync(
        string invitedEmail, string inviteeEmail, bool revoked = false, DateTimeOffset? createdAt = null)
    {
        var person = await AppRoleUsers.AddAsync(_fixture, inviteeEmail, "Invitee", "hash", confirmEmail: false);
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        var invitation = WorkspaceInvitation.Create(
            _a.AccountId, _a.WorkspaceId, invitedEmail, WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion, _a.User.Id,
            createdAt ?? DateTimeOffset.UtcNow.AddMinutes(-5));
        if (revoked) invitation.Revoke(_a.User.Id, DateTimeOffset.UtcNow);
        ctx.WorkspaceInvitations.Add(invitation);
        await ctx.SaveChangesAsync();
        return (person, issued.RawToken, invitation.Id);
    }

    private async Task<HttpResponseMessage> PostAsync(User person, string path, object? body)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(person, null);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
        var response = await client.PostAsJsonAsync(path, body);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string> ScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private Task<string> EmailConfirmedAsync(User person) =>
        ScalarAsync("SELECT email_confirmed::text FROM identity.users WHERE id = @u", ("u", person.Id));

    private Task<string> MembershipRowsAsync(User person) => ScalarAsync(
        "SELECT count(*) FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u",
        ("w", _a.WorkspaceId), ("u", person.Id));

    [Fact]
    public async Task AnUnconfirmedInvitee_AcceptingByTheEmailedLink_IsAdmitted_AndTheirEmailIsConfirmed()
    {
        var (person, token, _) = await InviteAsync("unconfirmed-ok@example.com", "unconfirmed-ok@example.com");
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");

        var response = await PostAsync(person, "/api/v1/invitations/accept", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("true");
        (await MembershipRowsAsync(person)).Should().Be("1");
    }

    [Fact]
    public async Task ATokenForAnotherAddress_IsRefused_AndConfirmsNothing()
    {
        var (person, token, _) = await InviteAsync("someone-else@example.com", "unconfirmed-other@example.com");

        var response = await PostAsync(person, "/api/v1/invitations/accept", new { token });

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");
        (await MembershipRowsAsync(person)).Should().Be("0");
    }

    [Fact]
    public async Task ARevokedToken_IsRefused_AndConfirmsNothing()
    {
        var (person, token, _) = await InviteAsync("unconfirmed-revoked@example.com", "unconfirmed-revoked@example.com", revoked: true);

        var response = await PostAsync(person, "/api/v1/invitations/accept", new { token });

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");
        (await MembershipRowsAsync(person)).Should().Be("0");
    }

    [Fact]
    public async Task AnExpiredToken_IsRefused_AndConfirmsNothing()
    {
        var (person, token, _) = await InviteAsync(
            "unconfirmed-expired@example.com", "unconfirmed-expired@example.com", createdAt: DateTimeOffset.UtcNow.AddDays(-30));

        var response = await PostAsync(person, "/api/v1/invitations/accept", new { token });

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");
    }

    [Fact]
    public async Task AnUnknownToken_IsRefused_AndConfirmsNothing()
    {
        var (person, _, _) = await InviteAsync("unconfirmed-unknown@example.com", "unconfirmed-unknown@example.com");
        var stranger = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation).RawToken;

        var response = await PostAsync(person, "/api/v1/invitations/accept", new { token = stranger });

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");
    }

    [Fact]
    public async Task AcceptingFromTheListById_WithAnUnconfirmedEmail_StaysRefused()
    {
        var (person, _, invitationId) = await InviteAsync("unconfirmed-byid@example.com", "unconfirmed-byid@example.com");

        var response = await PostAsync(person, $"/api/v1/invitations/{invitationId:D}/accept", null);

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await EmailConfirmedAsync(person)).Should().BeEquivalentTo("false");
        (await MembershipRowsAsync(person)).Should().Be("0");
    }
}

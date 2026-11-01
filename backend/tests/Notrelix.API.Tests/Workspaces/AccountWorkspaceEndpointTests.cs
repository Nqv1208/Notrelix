using MediatR;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaces;

namespace Notrelix.API.Tests.Workspaces;

/// <summary>
/// HTTP contract of the account workspace routes: list, default workspace and leave. The list and
/// leave handlers are replaced by fakes (their behaviour is proven in the Application and Integration
/// suites); the default-workspace route runs the real handler to prove the not-a-member answer.
/// </summary>
public sealed class AccountWorkspaceEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private static readonly Guid SoleOwnerWorkspace = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherWorkspace = Guid.Parse("50000000-0000-0000-0000-000000000002");

    private readonly NotrelixApiFactory _factory;
    private readonly HttpClient _client;

    public AccountWorkspaceEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
        _client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<GetMyWorkspacesQuery, Result<List<AccountWorkspaceDto>>>>();
                services.AddScoped<IRequestHandler<GetMyWorkspacesQuery, Result<List<AccountWorkspaceDto>>>, FakeListHandler>();
                services.RemoveAll<IRequestHandler<LeaveWorkspaceCommand, Result>>();
                services.AddScoped<IRequestHandler<LeaveWorkspaceCommand, Result>, FakeLeaveHandler>();
            }))
            .CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
    }

    [Fact]
    public async Task List_ReturnsTheRowsInTheContractShape_WithNullableCounts()
    {
        var response = await _client.GetAsync("/api/v1/account/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        rows.GetArrayLength().Should().Be(2);
        var owner = rows[0];
        owner.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["id", "name", "slug", "role", "memberCount", "departmentCount", "isDefault", "canLeave", "isArchived"]);
        owner.GetProperty("role").GetString().Should().Be("Owner");
        owner.GetProperty("memberCount").GetInt32().Should().Be(3);
        owner.GetProperty("canLeave").GetBoolean().Should().BeFalse();
        var guest = rows[1];
        guest.GetProperty("role").GetString().Should().Be("Guest");
        guest.GetProperty("memberCount").ValueKind.Should().Be(JsonValueKind.Null);
        guest.GetProperty("departmentCount").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SetDefault_ForAWorkspaceTheCallerIsNotAMemberOf_Is404()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/account/default-workspace", new { workspaceId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetDefault_WithAnEmptyWorkspaceId_Is400()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/account/default-workspace", new { workspaceId = Guid.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Leave_Succeeds_With204()
    {
        var response = await _client.DeleteAsync($"/api/v1/account/workspaces/{OtherWorkspace}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Leave_AsTheLastOwner_Is409_WithTheStableCode()
    {
        var response = await _client.DeleteAsync($"/api/v1/account/workspaces/{SoleOwnerWorkspace}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("workspaces.member.last-owner-cannot-leave");
    }

    [Theory]
    [InlineData("GET", "/api/v1/account/workspaces")]
    [InlineData("PUT", "/api/v1/account/default-workspace")]
    [InlineData("DELETE", "/api/v1/account/workspaces/50000000-0000-0000-0000-000000000002")]
    public async Task EveryAccountWorkspaceRoute_RequiresAuthentication(string method, string path)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { workspaceId = OtherWorkspace });
        }

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class FakeListHandler : IRequestHandler<GetMyWorkspacesQuery, Result<List<AccountWorkspaceDto>>>
    {
        public Task<Result<List<AccountWorkspaceDto>>> Handle(GetMyWorkspacesQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(Result<List<AccountWorkspaceDto>>.Success(
            [
                new AccountWorkspaceDto(SoleOwnerWorkspace, "Acme", "acme", "Owner", 3, 1, true, false),
                new AccountWorkspaceDto(OtherWorkspace, "Guest Space", "guest-space", "Guest", null, null, false, true),
            ]));
    }

    private sealed class FakeLeaveHandler : IRequestHandler<LeaveWorkspaceCommand, Result>
    {
        public Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken) =>
            request.WorkspaceId == SoleOwnerWorkspace
                ? throw new ConflictException("The last owner cannot leave.", LeaveWorkspaceCommandHandler.LastOwnerErrorCode)
                : Task.FromResult(Result.Success());
    }
}

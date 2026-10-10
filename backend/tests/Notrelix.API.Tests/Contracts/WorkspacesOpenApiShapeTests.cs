using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// Workspaces read contract used by the web Members, Settings and Home surfaces,
/// asserted on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json, regenerated and diffed by CI):
/// every Workspaces operation that returns a body declares its DTO instead of
/// <c>System.Void</c>, the workspace read exposes the aggregate <c>version</c>
/// the versioned writes require, and the member and invitation rows expose
/// <c>status</c>.
/// </summary>
public class WorkspacesOpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    [Theory]
    [InlineData("Workspaces.Workspaces.GetWorkspace", "200", "WorkspaceDto")]
    [InlineData("Workspaces.Workspaces.ListUserWorkspaces", "200", "WorkspaceDto[]")]
    [InlineData("Workspaces.Workspaces.GetAccountWorkspaces", "200", "WorkspaceDto[]")]
    [InlineData("Workspaces.Workspaces.ResolveSlug", "200", "WorkspaceDto")]
    [InlineData("Workspaces.Workspaces.CreateWorkspace", "201", "uuid")]
    [InlineData("Workspaces.Members.ListMembers", "200", "WorkspaceMemberDto[]")]
    [InlineData("Workspaces.Members.InviteMember", "201", "uuid")]
    [InlineData("Workspaces.Invitations.ListWorkspaceInvitations", "200", "WorkspaceInvitationDto[]")]
    [InlineData("Workspaces.Invitations.GetUserPendingInvitations", "200", "UserPendingInvitationDto[]")]
    [InlineData("Workspaces.Invitations.AcceptInvitation", "200", "AcceptInvitationResultDto")]
    [InlineData("Workspaces.Invitations.GetInvitationByToken", "200", "WorkspaceInvitationDto")]
    [InlineData("Workspaces.Invitations.ResendInvitation", "200", "uuid")]
    [InlineData("Workspaces.Settings.GetWorkspaceSettings", "200", "WorkspaceSettingsDto")]
    [InlineData("Workspaces.Spaces.CreateSpace", "201", "uuid")]
    [InlineData("Workspaces.Spaces.GetSpace", "200", "SpaceDto")]
    [InlineData("Workspaces.Spaces.GetWorkspaceSpaces", "200", "SpaceDto[]")]
    [InlineData("Workspaces.Teams.CreateTeam", "201", "uuid")]
    [InlineData("Workspaces.Teams.GetTeam", "200", "TeamDto")]
    [InlineData("Workspaces.Teams.GetWorkspaceTeams", "200", "TeamDto[]")]
    public void BodyReturningOperation_DeclaresItsResponseType(string operationId, string status, string expected)
    {
        var responses = Operation(operationId).GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().Contain(status, operationId);
        if (status != "200")
            responses.TryGetProperty("200", out _).Should().BeFalse($"{operationId} succeeds with {status}, not a phantom 200");

        var schema = responses.GetProperty(status).GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Describe(schema).Should().Be(expected, operationId);
    }

    [Theory]
    [InlineData("WorkspaceDto", "version")]
    [InlineData("WorkspaceMemberDto", "status")]
    [InlineData("WorkspaceInvitationDto", "status")]
    public void WorkspaceReadDto_ExposesField(string dto, string field)
    {
        var schema = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty($"Notrelix.Application.Features.Workspaces.DTOs.{dto}");
        schema.GetProperty("properties").TryGetProperty(field, out _).Should().BeTrue($"{dto} must expose {field}");
    }

    private static string Describe(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            return reference.GetString()!.Split('/').Last().Split('.').Last();
        if (schema.TryGetProperty("type", out var type) && type.GetString() == "array")
            return Describe(schema.GetProperty("items")) + "[]";
        if (schema.TryGetProperty("format", out var format))
            return format.GetString()!;
        return schema.GetProperty("type").GetString()!;
    }

    private static JsonElement Operation(string operationId)
    {
        foreach (var path in Document.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
                if (method.Value.TryGetProperty("operationId", out var id) && id.GetString() == operationId)
                    return method.Value;
        throw new InvalidOperationException($"Operation {operationId} is not in the contract.");
    }

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found (NRX-016)");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}

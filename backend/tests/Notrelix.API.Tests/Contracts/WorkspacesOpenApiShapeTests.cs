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
    [InlineData("Workspaces.Members.GetMyMembership", "200", "WorkspaceMemberDto")]
    [InlineData("Workspaces.Settings.GetWorkspaceSecurityExt", "200", "WorkspaceSecurityExtDto")]
    [InlineData("Workspaces.Teams.GetWorkspaceTeamsExt", "200", "WorkspaceTeamsExtDto")]
    [InlineData("Workspaces.Spaces.GetSpaceMembersExtra", "200", "SpaceMembersExtraDto")]
    [InlineData("Workspaces.Spaces.GetSpaceOverview", "200", "SpaceOverviewDto")]
    [InlineData("Workspaces.Search.CommandSearch", "200", "CommandSearchDto")]
    [InlineData("Workspaces.Workspaces.CheckWorkspaceSlug", "200", "WorkspaceSlugCheckDto")]
    [InlineData("Workspaces.Onboarding.GetOnboardingDraft", "200", "OnboardingDraftResponseDto")]
    [InlineData("Workspaces.Onboarding.GetGettingStarted", "200", "GettingStartedDto")]
    [InlineData("Workspaces.Members.InviteMember", "201", "uuid")]
    [InlineData("Workspaces.Members.UpdateMyJobTitle", "200", "WorkspaceMemberProfileDto")]
    [InlineData("Workspaces.Members.UpdateMemberJobTitle", "200", "WorkspaceMemberProfileDto")]
    [InlineData("Workspaces.Invitations.ListWorkspaceInvitations", "200", "WorkspaceInvitationDto[]")]
    [InlineData("Workspaces.Invitations.GetUserPendingInvitations", "200", "UserPendingInvitationDto[]")]
    [InlineData("Workspaces.Invitations.AcceptInvitation", "200", "AcceptInvitationResultDto")]
    [InlineData("Workspaces.Invitations.GetInvitationByToken", "200", "InvitationPreviewDto")]
    [InlineData("Workspaces.Invitations.ResendInvitation", "200", "uuid")]
    [InlineData("Workspaces.Invitations.ResendResourceInvitation", "200", "uuid")]
    [InlineData("Workspaces.Settings.GetWorkspaceSettings", "200", "WorkspaceSettingsDto")]
    [InlineData("Workspaces.Spaces.CreateSpace", "201", "uuid")]
    [InlineData("Workspaces.Spaces.GetSpace", "200", "SpaceDto")]
    [InlineData("Workspaces.Spaces.GetWorkspaceSpaces", "200", "SpaceDto[]")]
    [InlineData("Workspaces.Teams.CreateTeam", "201", "uuid")]
    [InlineData("Workspaces.Teams.GetTeam", "200", "TeamDto")]
    [InlineData("Workspaces.Teams.GetWorkspaceTeams", "200", "TeamDto[]")]
    [InlineData("Workspaces.Teams.GetTeamMembers", "200", "TeamMemberDto[]")]
    public void BodyReturningOperation_DeclaresItsResponseType(string operationId, string status, string expected)
    {
        var responses = Operation(operationId).GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().Contain(status, operationId);
        if (status != "200")
            responses.TryGetProperty("200", out _).Should().BeFalse($"{operationId} succeeds with {status}, not a phantom 200");

        var schema = responses.GetProperty(status).GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Describe(schema).Should().Be(expected, operationId);
    }

    [Fact]
    public void TeamMemberDto_ExposesTheWorkspaceJobTitle()
    {
        var schemas = Document.GetProperty("components").GetProperty("schemas");
        var team = schemas.EnumerateObject().Single(s => s.Name.EndsWith(".TeamMemberDto", StringComparison.Ordinal)).Value;
        team.GetProperty("properties").TryGetProperty("jobTitle", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("WorkspaceDto", "version")]
    [InlineData("WorkspaceMemberDto", "status")]
    [InlineData("WorkspaceMemberDto", "jobTitle")]
    [InlineData("WorkspaceMemberDto", "version")]
    [InlineData("WorkspaceMemberDto", "phone")]
    [InlineData("WorkspaceInvitationDto", "status")]
    public void WorkspaceReadDto_ExposesField(string dto, string field)
    {
        var schema = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty($"Notrelix.Application.Features.Workspaces.DTOs.{dto}");
        schema.GetProperty("properties").TryGetProperty(field, out _).Should().BeTrue($"{dto} must expose {field}");
    }

    [Theory]
    [InlineData("Workspaces.Workspaces.DeleteWorkspace")]
    [InlineData("Workspaces.Spaces.RemoveSpaceMember")]
    [InlineData("Workspaces.Teams.DeleteTeam")]
    [InlineData("Workspaces.Teams.RemoveTeamMember")]
    public void VersionedDelete_ReadsExpectedVersionFromTheQuery(string operationId)
    {
        // A body-less DELETE can only carry its precondition in the query string;
        // without a bound parameter the validator rejects every call.
        var parameters = Operation(operationId).GetProperty("parameters").EnumerateArray()
            .Where(p => p.GetProperty("name").GetString() == "expectedVersion")
            .ToArray();
        parameters.Should().ContainSingle(operationId);
        parameters[0].GetProperty("in").GetString().Should().Be("query");
        parameters[0].GetProperty("required").GetBoolean().Should().BeTrue();
    }

    [Theory]
    [InlineData("Workspaces.Teams.RenameTeam")]
    [InlineData("Workspaces.Teams.UpdateTeamDescription")]
    [InlineData("Workspaces.Teams.ArchiveTeam")]
    [InlineData("Workspaces.Teams.UnarchiveTeam")]
    [InlineData("Workspaces.Teams.DeleteTeam")]
    [InlineData("Workspaces.Teams.AddTeamMember")]
    [InlineData("Workspaces.Teams.RemoveTeamMember")]
    [InlineData("Workspaces.Teams.ChangeTeamMemberRole")]
    public void TeamWrite_Declares204(string operationId)
    {
        var responses = Operation(operationId).GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().Contain("204", operationId);
        responses.TryGetProperty("200", out _).Should().BeFalse($"{operationId} succeeds with 204, not a phantom 200");
    }

    [Theory]
    [InlineData("Workspaces.Teams.RenameTeam")]
    [InlineData("Workspaces.Teams.UpdateTeamDescription")]
    [InlineData("Workspaces.Teams.ArchiveTeam")]
    [InlineData("Workspaces.Teams.UnarchiveTeam")]
    [InlineData("Workspaces.Teams.AddTeamMember")]
    [InlineData("Workspaces.Teams.ChangeTeamMemberRole")]
    public void VersionedTeamWriteWithBody_CarriesExpectedVersionInTheBody(string operationId)
    {
        var schemaRef = Operation(operationId).GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        var schema = Document.GetProperty("components").GetProperty("schemas").GetProperty(schemaRef.Split('/').Last());

        var property = schema.GetProperty("properties").GetProperty("expectedVersion");
        property.GetProperty("type").GetString().Should().Be("integer", operationId);
        property.GetProperty("format").GetString().Should().Be("int64", operationId);
    }

    [Fact]
    public void CommandSearch_TakesTheTypedTextAsAnOptionalQueryParameter()
    {
        var q = Operation("Workspaces.Search.CommandSearch").GetProperty("parameters").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "q");
        q.GetProperty("in").GetString().Should().Be("query");
        (q.TryGetProperty("required", out var required) && required.GetBoolean()).Should().BeFalse("no text answers empty, not 400");
    }

    [Fact]
    public void RestoreTeam_StaysVersionless()
    {
        var operation = Operation("Workspaces.Teams.RestoreTeam");
        operation.TryGetProperty("requestBody", out _).Should().BeFalse();
        operation.GetProperty("parameters").EnumerateArray()
            .Should().NotContain(p => p.GetProperty("name").GetString() == "expectedVersion");
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

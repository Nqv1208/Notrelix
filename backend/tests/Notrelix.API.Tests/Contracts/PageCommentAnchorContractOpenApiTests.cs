using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The text anchor of a page comment on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json): an optional quoted range of one block on create, the same
/// range on the comment read, and the unchanged routes and existing fields.
/// </summary>
public class PageCommentAnchorContractOpenApiTests
{
    private const string RequestSchema = "Notrelix.API.Contracts.Collaboration.Comments.Requests.CreateCommentRequest";
    private const string AnchorRequestSchema = "Notrelix.API.Contracts.Collaboration.Comments.Requests.CommentAnchorRequest";
    private const string CommentSchema = "Notrelix.Application.Features.Collaboration.Comments.DTOs.CommentDto";
    private const string AnchorDtoSchema = "Notrelix.Application.Features.Collaboration.Comments.DTOs.CommentAnchorDto";
    private static readonly JsonElement Document = Load();
    private static JsonElement Schemas => Document.GetProperty("components").GetProperty("schemas");

    [Theory]
    [InlineData("/api/v1/pages/{pageId}/comments", "get")]
    [InlineData("/api/v1/pages/{pageId}/comments", "post")]
    public void PageCommentRoutes_AreUnchanged(string path, string verb)
    {
        Document.GetProperty("paths").GetProperty(path).TryGetProperty(verb, out _).Should().BeTrue($"{verb} {path}");
    }

    [Fact]
    public void CreateRequest_KeepsItsFields_AndAddsAnOptionalAnchor()
    {
        var properties = Schemas.GetProperty(RequestSchema).GetProperty("properties");

        properties.EnumerateObject().Select(p => p.Name).Should().Contain(["contentMd", "parentCommentId", "mentionedUserIds", "anchor"]);
        properties.GetProperty("anchor").GetProperty("$ref").GetString().Should().EndWith(AnchorRequestSchema);
        if (Schemas.GetProperty(RequestSchema).TryGetProperty("required", out var required))
            required.EnumerateArray().Select(e => e.GetString()).Should().NotContain("anchor", "an anchor is optional");
    }

    [Fact]
    public void Anchor_IsABlockAndAQuotedRange_OnWriteAndRead()
    {
        foreach (var schema in new[] { AnchorRequestSchema, AnchorDtoSchema })
        {
            Schemas.GetProperty(schema).GetProperty("properties").EnumerateObject().Select(p => p.Name)
                .Should().BeEquivalentTo(["blockId", "quote", "start", "end"], schema);
        }
    }

    [Fact]
    public void CommentDto_KeepsItsFields_AndCarriesTheAnchor()
    {
        var properties = Schemas.GetProperty(CommentSchema).GetProperty("properties");

        properties.EnumerateObject().Select(p => p.Name).Should().Contain(
            ["id", "userId", "userName", "contentMd", "parentCommentId", "isEdited", "resolvedAt", "createdAt", "anchor"]);
        properties.GetProperty("anchor").GetProperty("$ref").GetString().Should().EndWith(AnchorDtoSchema);
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        // Resolved from this source file, so the lookup does not depend on where build output goes.
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}

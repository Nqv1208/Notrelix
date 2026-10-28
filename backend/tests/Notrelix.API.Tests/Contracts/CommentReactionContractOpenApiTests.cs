using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Comment reactions on the checked-in producer contract: toggle routes under the item's comments, and the comment read carrying the summaries.</summary>
public class CommentReactionContractOpenApiTests
{
    private const string Route = "/api/v1/board-items/{boardItemId}/comments/{commentId}/reactions/{emoji}";
    private const string Prefix = "Notrelix.Application.Features.Collaboration.Comments.DTOs.";
    private static readonly JsonElement Document = Load();
    private static JsonElement Schemas => Document.GetProperty("components").GetProperty("schemas");

    [Fact]
    public void TheToggleRoute_OffersAddAndRemove_WithTheEmojiInThePath()
    {
        var path = Document.GetProperty("paths").GetProperty(Route);

        path.TryGetProperty("put", out _).Should().BeTrue();
        path.TryGetProperty("delete", out _).Should().BeTrue();
        path.GetProperty("put").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["boardItemId", "commentId", "emoji"]);
    }

    [Fact]
    public void TheCommentRead_CarriesReactionSummaries()
    {
        Schemas.GetProperty(Prefix + "CommentDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().Contain(["reactions", "parentCommentId"]);
        Schemas.GetProperty(Prefix + "ReactionSummaryDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["emoji", "count", "reactedByMe", "users"]);
        Schemas.GetProperty(Prefix + "ReactionUserDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["userId", "name"]);
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}

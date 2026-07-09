using Microsoft.Extensions.Options;

namespace Notrelix.Application.Tests.Common.Caching;

public class CacheIdentityHashTests
{
    private static CacheKeyFactory Factory => new(Options.Create(new CacheKeyOptions
    {
        Environment = "test",
        Prefix = "notrelix",
        SchemaVersion = 1
    }));

    private sealed record GetBoardCacheIdentity(Guid BoardId);
    private sealed record GetBoardSchemaCacheIdentity(Guid BoardId);
    private sealed record GetBoardSchemaCacheIdentity2(Guid BoardId);
    private sealed record ComplexIdentity(Guid BoardId, Guid WorkspaceId, string QueryName);

    [Fact]
    public void SameRecord_SameHash()
    {
        var id1 = new GetBoardCacheIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var id2 = new GetBoardCacheIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var hash1 = Factory.BuildHash(id1);
        var hash2 = Factory.BuildHash(id2);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void DifferentRecordTypes_WithSameShape_MayProduceSameHash()
    {
        var boardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var boardHash = Factory.BuildHash(new GetBoardSchemaCacheIdentity(boardId));
        var schemaHash = Factory.BuildHash(new GetBoardSchemaCacheIdentity2(boardId));

        // Same shape records serialize to same JSON; requestName in the key provides disambiguation
        boardHash.Should().Be(schemaHash);
    }

    [Fact]
    public void DifferentValues_DifferentHash()
    {
        var hash1 = Factory.BuildHash(new GetBoardCacheIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
        var hash2 = Factory.BuildHash(new GetBoardCacheIdentity(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")));

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComplexIdentity_IsDeterministic()
    {
        var id = new ComplexIdentity(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "GetBoardQuery");

        var hash1 = Factory.BuildHash(id);
        var hash2 = Factory.BuildHash(id);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_IsCaseSensitive()
    {
        var idA = Factory.BuildHash(new { Value = "abc" });
        var idB = Factory.BuildHash(new { Value = "ABC" });

        idA.Should().NotBe(idB);
    }

    [Fact]
    public void Hash_16Chars_LowercaseHex()
    {
        var hash = Factory.BuildHash(new GetBoardCacheIdentity(Guid.NewGuid()));

        hash.Should().HaveLength(16);
        hash.Should().MatchRegex("^[0-9a-f]{16}$");
        hash.ToUpperInvariant().Should().NotBe(hash);
    }
}

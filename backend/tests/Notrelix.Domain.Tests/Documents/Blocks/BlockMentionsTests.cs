using FluentAssertions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents;

public class BlockMentionsTests
{
    private static readonly Guid Ann = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    private static string Runs(params Guid[] mentions) =>
        "{\"v\":1,\"runs\":[" + string.Join(",", mentions.Select(m =>
            $"{{\"text\":\"@x\",\"marks\":[{{\"type\":\"mention\",\"userId\":\"{m}\"}}]}}")) + "]}";

    [Fact]
    public void Of_ReadsEveryMentionInTheRuns()
    {
        BlockMentions.Of(Runs(Ann, Bob), null).Should().BeEquivalentTo(new[] { Ann, Bob });
    }

    [Fact]
    public void Of_CountsAPersonMentionedTwiceOnce()
    {
        BlockMentions.Of(Runs(Ann, Ann)).Should().ContainSingle().Which.Should().Be(Ann);
    }

    [Fact]
    public void Of_ReadsMentionsInTableCellsAndCaptions()
    {
        var properties = "{\"table\":null,\"rows\":[[{\"runs\":[{\"text\":\"@x\",\"marks\":[{\"type\":\"mention\",\"userId\":\"" + Bob + "\"}]}]}]]," +
                         "\"caption\":[{\"text\":\"@y\",\"marks\":[{\"type\":\"mention\",\"userId\":\"" + Ann + "\"}]}]}";
        BlockMentions.Of("{}", properties).Should().BeEquivalentTo(new[] { Ann, Bob });
    }

    [Fact]
    public void Of_IgnoresOtherMarksAndUnreadableValues()
    {
        BlockMentions.Of("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"type\":\"bold\"}]}]}", "not json", null, "").Should().BeEmpty();
        BlockMentions.Of("{\"type\":\"mention\",\"userId\":\"nope\"}").Should().BeEmpty();
    }

    [Fact]
    public void Of_ABlock_ReadsItsStoredContent()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text,
            BlockContent.Create(JsonValue.Create(Runs(Ann))), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        BlockMentions.Of(block).Should().ContainSingle().Which.Should().Be(Ann);
    }
}

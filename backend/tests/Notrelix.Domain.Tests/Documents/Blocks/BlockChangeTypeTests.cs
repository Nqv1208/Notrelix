using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Tests.Documents;

public class BlockChangeTypeTests
{
    private const string Hello = "{\"v\":1,\"runs\":[{\"text\":\"Hello\",\"marks\":[{\"type\":\"bold\"}]}]}";

    private static Block NewBlock(BlockType type, string content, string properties = "{}")
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), type,
            BlockContent.Create(JsonValue.Create(content)), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            BlockProperties.Create(JsonValue.Create(properties)));
        ((IHasDomainEvents)block).ClearDomainEvents();
        return block;
    }

    [Fact]
    public void ChangeType_BetweenTextLikeTypes_KeepsIdAndRichText()
    {
        var block = NewBlock(BlockType.Text, Hello);
        var id = block.Id;
        var position = block.Position;
        var version = block.Version;

        block.ChangeType(BlockType.Heading2, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Id.Should().Be(id);
        block.Position.Should().Be(position);
        block.Type.Should().Be(BlockType.Heading2);
        block.Content.Data.Value.Should().Be(Hello);
        block.Version.Should().Be(version + 1);
        block.DomainEvents.OfType<BlockContentUpdatedDomainEvent>().Should().ContainSingle()
            .Which.BlockType.Should().Be(BlockType.Heading2);
    }

    [Fact]
    public void ChangeType_AwayFromToDo_DropsTheCheckedProperty_AndKeepsAlign()
    {
        var block = NewBlock(BlockType.ToDo, Hello, "{\"checked\":true,\"align\":\"center\"}");

        block.ChangeType(BlockType.Text, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Properties.Data.Value.Should().Be("{\"align\":\"center\"}");
        block.Content.Data.Value.Should().Be(Hello);
    }

    [Fact]
    public void ChangeType_ToCode_StripsTheMarks()
    {
        var block = NewBlock(BlockType.Text, Hello);

        block.ChangeType(BlockType.Code, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"Hello\",\"marks\":[]}]}");
    }

    [Fact]
    public void ChangeType_ToANonTextShape_StartsEmpty()
    {
        var block = NewBlock(BlockType.Text, Hello, "{\"align\":\"left\"}");

        block.ChangeType(BlockType.PageReference, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Type.Should().Be(BlockType.PageReference);
        block.Content.Data.Value.Should().Be("{}");
        block.Properties.Data.Value.Should().Be("{}");
    }

    [Fact]
    public void ChangeType_FromANonTextShape_StartsEmpty()
    {
        var target = Guid.NewGuid();
        var block = NewBlock(BlockType.PageReference, "{}", $"{{\"pageId\":\"{target}\"}}");

        block.ChangeType(BlockType.Callout, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Type.Should().Be(BlockType.Callout);
        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[]}");
        block.Properties.Data.Value.Should().Be("{}");
    }

    [Fact]
    public void ChangeType_ToTheSameType_IsANoOp()
    {
        var block = NewBlock(BlockType.Quote, Hello);
        var version = block.Version;

        block.ChangeType(BlockType.Quote, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Version.Should().Be(version);
        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeType_OnADeletedBlock_Throws()
    {
        var block = NewBlock(BlockType.Text, "{}");
        block.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.ChangeType(BlockType.Quote, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<Exception>();
        block.Type.Should().Be(BlockType.Text);
    }

    [Fact]
    public void PageReference_WithAMalformedTarget_IsRejected()
    {
        var act = () => NewBlock(BlockType.PageReference, "{}", "{\"pageId\":\"not-a-page\"}");

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PageReferenceTargetInvalid);
    }

    [Fact]
    public void PageReference_WithAPageId_IsAccepted()
    {
        var act = () => NewBlock(BlockType.PageReference, "{}", $"{{\"pageId\":\"{Guid.NewGuid()}\"}}");

        act.Should().NotThrow();
    }
}

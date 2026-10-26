using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Tests.Documents;

public class BlockEditTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    private static Block NewBlock(BlockType type = BlockType.Text, string content = "{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[]}]}")
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), type,
            BlockContent.Create(JsonValue.Create(content)), FractionalIndex.Create("a0"), Guid.NewGuid(), Now);
        ((IHasDomainEvents)block).ClearDomainEvents();
        return block;
    }

    private static BlockContent Content(string json) => BlockContent.Create(JsonValue.Create(json));
    private static BlockProperties Props(string json) => BlockProperties.Create(JsonValue.Create(json));

    [Fact]
    public void Edit_ContentAndProperties_IsOneVersionStep_AndRaisesBothEvents()
    {
        var block = NewBlock(BlockType.ToDo);
        var version = block.Version;

        block.Edit(null, Content("{\"v\":1,\"runs\":[{\"text\":\"b\",\"marks\":[]}]}"), Props("{\"checked\":true}"), Guid.NewGuid(), Now);

        block.Version.Should().Be(version + 1);
        block.Content.Data.Value.Should().Contain("\"b\"");
        block.Properties.Data.Value.Should().Be("{\"checked\":true}");
        block.DomainEvents.OfType<BlockContentUpdatedDomainEvent>().Should().ContainSingle();
        block.DomainEvents.OfType<BlockPropertiesUpdatedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Edit_WithNothingChanged_IsANoOp()
    {
        var block = NewBlock();
        var version = block.Version;

        block.Edit(null, Content("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[]}]}"), Props("{}"), Guid.NewGuid(), Now);

        block.Version.Should().Be(version);
        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Edit_TheSameTextWithMembersInAnotherOrder_IsANoOp()
    {
        var block = NewBlock(content: "{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"type\":\"link\",\"href\":\"https://a.io\"}]}]}");
        var version = block.Version;

        // What a database hands back: members shortest key first.
        block.Edit(null,
            Content("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"href\":\"https://a.io\",\"type\":\"link\"}]}]}"),
            null, Guid.NewGuid(), Now);

        block.Version.Should().Be(version);
    }

    [Fact]
    public void Edit_WithInvalidContent_ChangesNothing()
    {
        var block = NewBlock();
        var before = block.Content.Data.Value;
        var version = block.Version;

        var act = () => block.Edit(BlockType.Heading1, Content("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[{\"type\":\"link\",\"href\":\"javascript:alert(1)\"}]}]}"), null, Guid.NewGuid(), Now);

        act.Should().Throw<BusinessRuleException>();
        block.Type.Should().Be(BlockType.Text);
        block.Content.Data.Value.Should().Be(before);
        block.Version.Should().Be(version);
        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Edit_WithPropertiesTheTypeLacks_ChangesNothing()
    {
        var block = NewBlock();
        var version = block.Version;

        var act = () => block.Edit(null, null, Props("{\"checked\":true}"), Guid.NewGuid(), Now);

        act.Should().Throw<BusinessRuleException>();
        block.Version.Should().Be(version);
    }

    [Fact]
    public void Edit_RetypingAndSettingTheNewTypesProperties_ValidatesAgainstTheNewType()
    {
        var block = NewBlock();

        block.Edit(BlockType.ToDo, null, Props("{\"checked\":true}"), Guid.NewGuid(), Now);

        block.Type.Should().Be(BlockType.ToDo);
        block.Properties.Data.Value.Should().Be("{\"checked\":true}");
        block.Content.Data.Value.Should().Contain("\"a\"");
    }

    [Fact]
    public void Edit_OnADeletedBlock_Throws()
    {
        var block = NewBlock();
        block.Delete(Guid.NewGuid(), Now);

        var act = () => block.Edit(null, Content("{}"), null, Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateRoot_WithAClientId_UsesIt()
    {
        var id = Guid.NewGuid();

        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text,
            Content("{}"), FractionalIndex.Create("a0"), Guid.NewGuid(), Now, id: id);

        block.Id.Should().Be(id);
    }

    [Fact]
    public void Create_StoresTheCanonicalContent_AndProperties()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text,
            Content("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"type\":\"italic\"},{\"type\":\"bold\"}]}]}"),
            FractionalIndex.Create("a0"), Guid.NewGuid(), Now, Props("{\"align\":\"left\"}"));

        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"type\":\"bold\"},{\"type\":\"italic\"}]}]}");
    }

    [Fact]
    public void Create_WithInvalidProperties_IsRejected()
    {
        var act = () => Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Divider,
            Content("{}"), FractionalIndex.Create("a0"), Guid.NewGuid(), Now, Props("{\"align\":\"left\"}"));

        act.Should().Throw<BusinessRuleException>();
    }
}

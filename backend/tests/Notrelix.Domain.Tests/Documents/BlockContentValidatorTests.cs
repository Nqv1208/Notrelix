using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class BlockContentValidatorTests
{
    [Fact]
    public void Validate_WithNonNullData_ShouldNotThrow()
    {
        var content = BlockContent.Create(JsonValue.EmptyObject());
        var act = () => BlockContentValidator.Validate(BlockType.Text, content);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithInvalidType_ShouldNotThrow_CurrentlyUnvalidated()
    {
        var content = BlockContent.Create(JsonValue.Null());
        var act = () => BlockContentValidator.Validate(BlockType.Text, content);
        act.Should().NotThrow();
    }
}

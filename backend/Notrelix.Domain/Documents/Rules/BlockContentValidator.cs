using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Documents.Rules;

public static class BlockContentValidator
{
    public static void Validate(Blocks.BlockType type, Blocks.BlockContent content)
    {
        // For now, we just ensure content is not null (already handled by Guard in Block.cs)
        // In a real implementation, we would validate the JSON schema based on the BlockType.
        if (content.Data.Value == null)
            throw new BusinessRuleException("Block content data cannot be null.");
    }
}

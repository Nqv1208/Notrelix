using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemLink : Entity
{
    public Guid SourceItemId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public BoardItemLinkType LinkType { get; private set; }

    private BoardItemLink() : base() { }

    public static BoardItemLink Create(Guid sourceItemId, ResourceRef target, BoardItemLinkType type)
    {
        return new BoardItemLink
        {
            SourceItemId = sourceItemId,
            Target = target,
            LinkType = type
        };
    }
}

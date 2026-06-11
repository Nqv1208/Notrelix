using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemLabel : Entity
{
    public Guid ItemId { get; private set; }
    public Guid LabelId { get; private set; }

    private BoardItemLabel() : base() { }

    public static BoardItemLabel Create(Guid itemId, Guid labelId)
    {
        return new BoardItemLabel
        {
            ItemId = itemId,
            LabelId = labelId
        };
    }
}

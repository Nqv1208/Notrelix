using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations;

public class RelationFieldConfig : Entity
{
    public Guid FieldId { get; private set; }
    public Guid SourceBoardId { get; private set; }
    public Guid TargetBoardId { get; private set; }
    public bool AllowMultiple { get; private set; }
    public bool CreateBacklink { get; private set; }
    public Guid? BacklinkFieldId { get; private set; }
    public RelationDirection Direction { get; private set; }

    private RelationFieldConfig() : base() { }

    public static RelationFieldConfig Create(Guid fieldId, Guid sourceBoardId, Guid targetBoardId, RelationDirection direction)
    {
        Guard.NotEmpty(fieldId);
        Guard.NotEmpty(sourceBoardId);
        Guard.NotEmpty(targetBoardId);

        return new RelationFieldConfig
        {
            FieldId = fieldId,
            SourceBoardId = sourceBoardId,
            TargetBoardId = targetBoardId,
            Direction = direction,
            AllowMultiple = true,
            CreateBacklink = true
        };
    }
}

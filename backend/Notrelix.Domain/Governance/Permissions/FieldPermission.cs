using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Governance.Permissions;

public class FieldPermission : Entity
{
    public Guid FieldId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public PermissionLevel Level { get; private set; }

    private FieldPermission() : base() { }

    public static FieldPermission Grant(Guid fieldId, PermissionSubjectType subjectType, Guid subjectId, PermissionLevel level)
    {
        Guard.NotEmpty(fieldId);
        Guard.NotEmpty(subjectId);

        return new FieldPermission
        {
            FieldId = fieldId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level
        };
    }
}

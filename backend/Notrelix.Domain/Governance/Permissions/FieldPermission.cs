using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Governance.Permissions;

public class FieldPermission : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid FieldId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public bool CanView { get; private set; } = true;
    public bool CanEdit { get; private set; }
    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;
    public bool CanMask { get; private set; }
    public string ConditionJson { get; private set; } = "{}";
    public long Version { get; private set; } = 1;

    private FieldPermission() : base() { }

    public static FieldPermission Grant(
        Guid workspaceId,
        Guid boardId,
        Guid fieldId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        bool canView,
        bool canEdit,
        PermissionEffect effect = PermissionEffect.Allow,
        bool canMask = false,
        string? conditionJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(fieldId);
        Guard.NotEmpty(subjectId);

        return new FieldPermission
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            FieldId = fieldId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            CanView = canView,
            CanEdit = canEdit,
            Effect = effect,
            CanMask = canMask,
            ConditionJson = conditionJson ?? "{}"
        };
    }
}

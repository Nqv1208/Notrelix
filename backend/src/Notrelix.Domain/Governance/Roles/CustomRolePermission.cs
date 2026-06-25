namespace Notrelix.Domain.Governance.Roles;

public class CustomRolePermission : Entity
{
    public Guid CustomRoleId { get; private set; }
    public string Action { get; private set; } = null!;
    public bool IsAllowed { get; private set; }
    public JsonValue Conditions { get; private set; } = null!;

    private CustomRolePermission() : base() { }

    public static CustomRolePermission Create(Guid customRoleId, string action, bool isAllowed = true, JsonValue? conditions = null)
    {
        Guard.NotEmpty(customRoleId);
        Guard.NotNullOrWhiteSpace(action);

        return new CustomRolePermission
        {
            CustomRoleId = customRoleId,
            Action = action,
            IsAllowed = isAllowed,
            Conditions = conditions ?? JsonValue.EmptyObject()
        };
    }
}

namespace Notrelix.Application.Features.Operators.Public.Principal;

/// <summary>
/// The platform operator who made the current operator-console request. It exists only on operator routes,
/// after the operator's token was verified and the operator row was found active; it is never a tenant user.
/// </summary>
public interface ICurrentOperator
{
    Guid OperatorId { get; }
    string Email { get; }
    string DisplayName { get; }
}

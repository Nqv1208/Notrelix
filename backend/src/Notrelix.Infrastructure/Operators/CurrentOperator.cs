using Notrelix.Application.Features.Operators.Public.Principal;

namespace Notrelix.Infrastructure.Operators;

/// <summary>
/// The operator of the current request. Empty until the operator session check set it; reading an unset
/// operator is a fault, so no operator action can run without one.
/// </summary>
public sealed class CurrentOperator : ICurrentOperator
{
    private Guid? _id;
    private string _email = string.Empty;
    private string _displayName = string.Empty;

    public Guid OperatorId => _id ?? throw new InvalidOperationException("No operator is set for this request.");
    public string Email => _email;
    public string DisplayName => _displayName;
    public bool IsSet => _id.HasValue;

    public void Set(Guid operatorId, string email, string displayName)
    {
        _id = operatorId;
        _email = email;
        _displayName = displayName;
    }
}

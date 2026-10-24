using Notrelix.Application.Common.Data.Rls;

namespace Notrelix.Application.Features.Identity.Registration.Services;

/// <summary>
/// Enters the database registration scope for the one new identity a registration request creates. The
/// scope is bound to that user id (which must not exist yet) and, through the first grant written in it, to
/// the one new account; it ends with the request transaction. A host that sets no database session context
/// has nothing to enter.
/// </summary>
public sealed class RegistrationScope
{
    private static readonly SystemOperationReason Reason =
        new("Registration", "a new identity and its personal account are created before any session exists");

    private readonly IRlsSessionContext _rls;

    public RegistrationScope(IRlsSessionContext rls)
    {
        _rls = rls;
    }

    public async Task EnterAsync(Guid newUserId, CancellationToken cancellationToken)
    {
        if (!_rls.IsSessionContextEnabled)
            return;

        if (!await _rls.EnterRegistrationScopeAsync(newUserId, Reason, cancellationToken))
        {
            throw new InvalidOperationException(
                "The registration scope could not be entered: the identity already exists or the invitation is no longer pending.");
        }
    }
}

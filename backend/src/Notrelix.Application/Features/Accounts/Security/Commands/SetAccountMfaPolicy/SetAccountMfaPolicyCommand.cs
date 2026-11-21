using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;

/// <summary>
/// Turns the Account's MFA requirement on or off. Only an Account Owner may do it (ManageAccountSecurity is Owner-locked: no role carries it).
/// </summary>
public record SetAccountMfaPolicyCommand(bool Required)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

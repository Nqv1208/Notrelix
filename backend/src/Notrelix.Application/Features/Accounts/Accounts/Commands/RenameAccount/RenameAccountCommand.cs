using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;

public record RenameAccountCommand(string Name)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
}

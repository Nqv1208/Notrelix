using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;

public record RenameAccountCommand(Guid AccountId, string Name, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("accounts.account"), AccountId);
}

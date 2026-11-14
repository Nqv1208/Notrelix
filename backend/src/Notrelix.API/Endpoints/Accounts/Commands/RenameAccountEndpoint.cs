using Notrelix.API.Contracts.Accounts.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;

namespace Notrelix.API.Endpoints.Accounts.Commands;

public static class RenameAccountEndpoint
{
    public static IEndpointRouteBuilder MapRenameAccount(this IEndpointRouteBuilder group)
    {
        group.MapAccountPut("/rename", HandleAsync)
            .WithName("Accounts.RenameAccount")
            .WithTags("Accounts")
            .WithSummary("Rename the current account");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RenameAccountRequest request,
        ICurrentRequestContext requestContext,
        ISender sender)
    {
        var result = await sender.Send(new RenameAccountCommand(
            requestContext.RequireAccountId(), request.Name, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}

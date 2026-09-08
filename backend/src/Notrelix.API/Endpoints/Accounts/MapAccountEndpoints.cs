using Notrelix.API.Endpoints.Accounts.Commands;

namespace Notrelix.API.Endpoints.Accounts;

public static class MapAccountEndpoints
{
    public static IEndpointRouteBuilder RegisterAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/accounts")
            .WithTags("Accounts");

        group.MapRenameAccount();

        return app;
    }
}

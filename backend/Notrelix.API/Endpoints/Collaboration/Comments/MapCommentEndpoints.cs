using Notrelix.API.Endpoints.Collaboration.Comments.Commands;
using Notrelix.API.Endpoints.Collaboration.Comments.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Comments;

public static class MapCommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentsEndpoints(this IEndpointRouteBuilder app)
    {
        var cardGroup = app
            .MapGroup("/api/v1/cards/{cardId:guid}/comments")
            .WithTags("Collaboration.Comments")
            .RequireAuthorization()
            .WithOpenApi();

        cardGroup.MapGetCardComments();
        cardGroup.MapCreateCardComment();

        var pageGroup = app
            .MapGroup("/api/v1/pages/{pageId:guid}/comments")
            .WithTags("Collaboration.Comments")
            .RequireAuthorization()
            .WithOpenApi();

        pageGroup.MapGetPageComments();
        pageGroup.MapCreatePageComment();

        var group = app
            .MapGroup("/api/v1/comments")
            .WithTags("Collaboration.Comments")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapUpdateComment();
        group.MapDeleteComment();
        group.MapResolveComment();

        return app;
    }
}

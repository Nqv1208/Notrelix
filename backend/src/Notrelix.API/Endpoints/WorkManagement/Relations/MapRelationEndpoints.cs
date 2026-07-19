using Notrelix.API.Endpoints.WorkManagement.Relations.Commands;
using Notrelix.API.Endpoints.WorkManagement.Relations.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Relations;

public static class MapRelationEndpoints
{
    public static IEndpointRouteBuilder MapRelations(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/relations")
            .WithTags("WorkManagement.Relations")
            .WithOpenApi();

        boardGroup.MapCreateBoardRelation();
        boardGroup.MapListBoardRelations();

        var group = app
            .MapGroup("/api/v1/relations/{relationId:guid}")
            .WithTags("WorkManagement.Relations")
            .WithOpenApi();

        group.MapDeleteBoardRelation();
        group.MapPauseBoardRelation();
        group.MapResumeBoardRelation();

        return app;
    }
}

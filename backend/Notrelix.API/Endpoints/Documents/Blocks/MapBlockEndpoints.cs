using Notrelix.API.Endpoints.Documents.Blocks.Commands;
using Notrelix.API.Endpoints.Documents.Blocks.Queries;

namespace Notrelix.API.Endpoints.Documents.Blocks;

public static class MapBlockEndpoints
{
    public static IEndpointRouteBuilder AddBlockEndpoints(this IEndpointRouteBuilder app)
    {
        var pageBlocksGroup = app
            .MapGroup("/api/v1/pages/{pageId:guid}/blocks")
            .WithTags("Documents.Blocks")
            .RequireAuthorization()
            .WithOpenApi();

        pageBlocksGroup.MapListPageBlocks();
        pageBlocksGroup.MapCreateBlock();
        pageBlocksGroup.MapBatchUpdateBlocks();

        var blockByIdGroup = app
            .MapGroup("/api/v1/blocks/{blockId:guid}")
            .WithTags("Documents.Blocks")
            .RequireAuthorization()
            .WithOpenApi();

        blockByIdGroup.MapUpdateBlock();
        blockByIdGroup.MapDeleteBlock();

        var blocksGroup = app
            .MapGroup("/api/v1/blocks")
            .WithTags("Documents.Blocks")
            .RequireAuthorization()
            .WithOpenApi();

        blocksGroup.MapReorderBlocks();

        return app;
    }
}

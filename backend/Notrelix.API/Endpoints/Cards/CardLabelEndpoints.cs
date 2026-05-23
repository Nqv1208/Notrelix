using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;

namespace Notrelix.API.Endpoints.Cards;

public static class CardLabelEndpoints
{
    public static IEndpointRouteBuilder MapCardLabelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/cards/{cardId:guid}/labels")
            .WithTags("Card Labels")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/", AddLabel)
            .WithName("AddLabelToCard")
            .WithSummary("Add a label to card");

        group.MapDelete("/{labelId:guid}", RemoveLabel)
            .WithName("RemoveLabelFromCard")
            .WithSummary("Remove a label from card");

        return app;
    }

    private static async Task<IResult> AddLabel(Guid cardId, AddCardLabelRequest body, ISender sender)
    {
        var result = await sender.Send(new AddLabelToCardCommand(cardId, body.LabelId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> RemoveLabel(Guid cardId, Guid labelId, ISender sender)
    {
        var result = await sender.Send(new RemoveLabelFromCardCommand(cardId, labelId));
        return result.ToNoContentResult();
    }
}

public record AddCardLabelRequest(Guid LabelId);

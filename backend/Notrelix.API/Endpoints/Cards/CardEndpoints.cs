using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Cards;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        // ── List-scoped routes ───────────────────────────────────
        var listGroup = app
            .MapGroup("/api/lists/{listId:guid}/cards")
            .WithTags("Cards")
            .RequireAuthorization()
            .WithOpenApi();

        listGroup.MapPost("/", CreateCard)
            .WithName("CreateCard")
            .WithSummary("Create a new card in list");

        // ── Card-scoped routes ───────────────────────────────────
        var group = app
            .MapGroup("/api/cards")
            .WithTags("Cards")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/{cardId:guid}", GetCard)
            .WithName("GetCard")
            .WithSummary("Get card detail with members, labels, checklists");

        group.MapPatch("/{cardId:guid}", UpdateCard)
            .WithName("UpdateCard")
            .WithSummary("Update card properties");

        group.MapDelete("/{cardId:guid}", DeleteCard)
            .WithName("DeleteCard")
            .WithSummary("Soft delete a card");

        group.MapPost("/{cardId:guid}/move", MoveCard)
            .WithName("MoveCard")
            .WithSummary("Move card to another list/position");

        group.MapPost("/{cardId:guid}/archive", ArchiveCard)
            .WithName("ArchiveCard")
            .WithSummary("Archive a card");

        group.MapPost("/{cardId:guid}/link-page", LinkPageToCard)
            .WithName("LinkPageToCard")
            .WithSummary("Link a Notion page to this card");

        group.MapDelete("/{cardId:guid}/link-page", UnlinkPageFromCard)
            .WithName("UnlinkPageFromCard")
            .WithSummary("Unlink page from card");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> CreateCard(Guid listId, CreateCardRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCardCommand(listId, body.Title, body.Position));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetCard(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new GetCardQuery(cardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateCard(Guid cardId, UpdateCardCommand command, ISender sender)
    {
        var cmd = command with { CardId = cardId };
        var result = await sender.Send(cmd);
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteCard(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new ArchiveCardCommand(cardId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> MoveCard(Guid cardId, MoveCardRequest body, ISender sender)
    {
        var result = await sender.Send(new MoveCardCommand(cardId, body.ListId, body.Position));
        return result.ToApiResult();
    }

    private static async Task<IResult> ArchiveCard(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new ArchiveCardCommand(cardId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> LinkPageToCard(Guid cardId, LinkPageRequest body, ISender sender)
    {
        var result = await sender.Send(new LinkPageToCardCommand(cardId, body.PageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UnlinkPageFromCard(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new UnlinkPageFromCardCommand(cardId));
        return result.ToNoContentResult();
    }
}

public record CreateCardRequest(string Title, double? Position = null);
public record MoveCardRequest(Guid ListId, double Position);
public record LinkPageRequest(Guid PageId);

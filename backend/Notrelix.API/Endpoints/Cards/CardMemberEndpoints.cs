using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;

namespace Notrelix.API.Endpoints.Cards;

public static class CardMemberEndpoints
{
    public static IEndpointRouteBuilder MapCardMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/cards/{cardId:guid}/members")
            .WithTags("Card Members")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/", AssignMember)
            .WithName("AssignCardMember")
            .WithSummary("Assign a member to card");

        group.MapDelete("/{userId:guid}", UnassignMember)
            .WithName("UnassignCardMember")
            .WithSummary("Unassign a member from card");

        return app;
    }

    private static async Task<IResult> AssignMember(Guid cardId, AssignMemberRequest body, ISender sender)
    {
        var result = await sender.Send(new AssignCardMemberCommand(cardId, body.UserId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> UnassignMember(Guid cardId, Guid userId, ISender sender)
    {
        var result = await sender.Send(new UnassignCardMemberCommand(cardId, userId));
        return result.ToNoContentResult();
    }
}

public record AssignMemberRequest(Guid UserId);

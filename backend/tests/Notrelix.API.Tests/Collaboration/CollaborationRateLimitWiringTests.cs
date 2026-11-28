using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.RateLimits;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;

namespace Notrelix.API.Tests.Collaboration;

/// <summary>
/// UC-COL-H05: the four limited handlers take the limiter as an optional dependency, so a missing registration would silently
/// switch the limits off. This resolves them from the API's own container and checks each one holds the limiter.
/// </summary>
public class CollaborationRateLimitWiringTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public CollaborationRateLimitWiringTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public void The_comment_reaction_and_upload_handlers_are_built_with_the_limiter()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<ICollaborationRateLimiter>().Should().BeOfType<CollaborationRateLimiter>();
        Limiter(services.GetRequiredService<IRequestHandler<CreateCommentCommand, Result<Guid>>>()).Should().NotBeNull();
        Limiter(services.GetRequiredService<IRequestHandler<AddReactionCommand, Result>>()).Should().NotBeNull();
        Limiter(services.GetRequiredService<IRequestHandler<UploadBoardItemAttachmentCommand, Result<AttachmentDto>>>()).Should().NotBeNull();
        Limiter(services.GetRequiredService<IRequestHandler<UploadPageAttachmentCommand, Result<AttachmentDto>>>()).Should().NotBeNull();
    }

    private static object? Limiter(object handler) =>
        handler.GetType().GetField("_rateLimits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(handler);
}

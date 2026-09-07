using Notrelix.Application.EventMappers.Collaboration;
using Notrelix.Application.Events.Collaboration;
using Notrelix.Domain.Collaboration.Comments.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>
/// TAC-DC-005 mapper contract — the producer-owned comment mapper translates
/// the owned Domain fact into the registry identity "comment.created" V1 with
/// the authoritative tenant envelope, the canonical resource kind string, and
/// the real comment content.
/// </summary>
public sealed class CommentEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CommentCreated_MapsCanonicalKindAndOwnedContent()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var authorId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var domainEvent = new CommentCreatedDomainEvent(
            accountId,
            workspaceId,
            commentId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.CreateVersion7(), workspaceId),
            authorId,
            "Mapper content fact",
            Now);

        var mapped = new CommentEventMapper().Map(domainEvent);

        mapped.Should().NotBeNull();
        mapped!.AccountId.Should().Be(accountId);
        mapped.WorkspaceId.Should().Be(workspaceId);
        mapped.CommentId.Should().Be(commentId);
        mapped.AuthorId.Should().Be(authorId);
        mapped.TargetType.Should().Be("work-management.board-item");
        mapped.Body.Should().Be("Mapper content fact");
        mapped.CorrelationId.Should().Be(domainEvent.EventId);
        mapped.ActorUserId.Should().Be(authorId);
        mapped.OccurredAt.Should().Be(Now);
    }

    [Fact]
    public void CommentCreated_PageTarget_MapsCanonicalPageKind()
    {
        var workspaceId = Guid.CreateVersion7();
        var domainEvent = new CommentCreatedDomainEvent(
            Guid.CreateVersion7(),
            workspaceId,
            Guid.CreateVersion7(),
            ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.CreateVersion7(), workspaceId),
            Guid.CreateVersion7(),
            "Page comment",
            Now);

        var mapped = new CommentEventMapper().Map(domainEvent);

        mapped!.TargetType.Should().Be("documents.page");
    }

    [Fact]
    public void CommentCreated_KeepsRegistryIdentity()
    {
        var attribute = typeof(CommentCreatedIntegrationEvent)
            .GetCustomAttributes(true)
            .OfType<EventNameAttribute>()
            .Single();

        attribute.Name.Should().Be("comment.created");
        attribute.Version.Should().Be(1);
    }
}

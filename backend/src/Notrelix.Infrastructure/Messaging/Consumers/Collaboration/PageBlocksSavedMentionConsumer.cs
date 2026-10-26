using System.Security.Cryptography;
using System.Text;
using Notrelix.Application.Events.Documents;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Infrastructure.Data;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>
/// A person newly mentioned inside a page block is told once: this turns each mention a committed save added
/// into the same Mention the comment path creates, which the existing notification consumer then delivers. It
/// notifies only people who can view the page and never the person who wrote the mention. Each Mention is keyed
/// by (save, block, person), so a redelivered save adds nothing, and removing a mention later retracts nothing.
/// </summary>
[OrderingNotApplicable("Each mention is keyed by its save, block and person; mentions are independent and never supersede each other.")]
public sealed class PageBlocksSavedMentionConsumer : IConsumer<PageBlocksSavedIntegrationEvent>
{
    private const string PageKind = "documents.page";

    private readonly ICollaborationDbContext _collaboration;
    private readonly ApplicationDbContext _context;
    private readonly IPageViewAccess _pageAccess;
    private readonly ILogger<PageBlocksSavedMentionConsumer> _logger;

    public PageBlocksSavedMentionConsumer(
        ICollaborationDbContext collaboration,
        ApplicationDbContext context,
        IPageViewAccess pageAccess,
        ILogger<PageBlocksSavedMentionConsumer> logger)
    {
        _collaboration = collaboration;
        _context = context;
        _pageAccess = pageAccess;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PageBlocksSavedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        if (msg.Mentions.Count == 0) return;

        if (msg.AccountId is not { } accountId || accountId == Guid.Empty
            || msg.WorkspaceId is not { } workspaceId || msg.ActorUserId is not { } author || author == Guid.Empty)
        {
            _logger.LogWarning("PageBlocksSaved {EventId} has no authoritative account, workspace and author; mentions skipped", msg.EventId);
            return;
        }

        var source = ResourceRef.Create(ResourceKind.Create(PageKind), msg.PageId, workspaceId);
        var viewable = new Dictionary<Guid, bool>();
        var added = 0;

        foreach (var mention in msg.Mentions.DistinctBy(m => (m.BlockId, m.UserId)))
        {
            if (mention.UserId == author) continue;

            var id = MentionId(msg.CorrelationId, msg.PageId, mention.BlockId, mention.UserId);
            if (await _collaboration.PageMentions.AnyAsync(m => m.Id == id, ct)) continue;

            if (!viewable.TryGetValue(mention.UserId, out var canView))
            {
                canView = await CanViewAsync(accountId, workspaceId, msg.PageId, mention.UserId, ct);
                viewable[mention.UserId] = canView;
            }

            if (!canView) continue;

            _collaboration.PageMentions.Add(Mention.Create(
                accountId, workspaceId, source, MentionType.User, mention.UserId, author, msg.OccurredAt, msg.PageTitle, id));
            added++;
        }

        if (added == 0) return;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Recorded {Count} mention(s) in page {PageId}", added, msg.PageId);
    }

    private async Task<bool> CanViewAsync(Guid accountId, Guid workspaceId, Guid pageId, Guid userId, CancellationToken ct)
    {
        try
        {
            return await _pageAccess.CanViewAsync(accountId, workspaceId, pageId, userId, ct);
        }
        catch (Exception ex) when (ex is DomainException or Notrelix.Application.Common.Exceptions.NotFoundException or Notrelix.Application.Common.Exceptions.ForbiddenException)
        {
            return false;
        }
    }

    /// <summary>The same save, block and person always give the same id, so a redelivery finds its mention already recorded.</summary>
    public static Guid MentionId(Guid saveId, Guid pageId, Guid blockId, Guid userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"page-block-mention:{saveId:N}:{pageId:N}:{blockId:N}:{userId:N}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

public sealed class PageBlocksSavedMentionConsumerDefinition : ConsumerDefinition<PageBlocksSavedMentionConsumer>
{
    public PageBlocksSavedMentionConsumerDefinition()
    {
        EndpointName = "notrelix-collab-page-blocks-saved-mention-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PageBlocksSavedMentionConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
            r.Ignore<DomainException>();
        });
    }
}

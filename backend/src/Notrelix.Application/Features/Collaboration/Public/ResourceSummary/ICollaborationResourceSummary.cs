namespace Notrelix.Application.Features.Collaboration.Public.ResourceSummary;

/// <summary>
/// Producer-owned read source for collaboration resource summaries. Consumers
/// compose this published semantic boundary into their own read models; they
/// never query Collaboration persistence directly. Consumers address resources
/// by (kind, id) pairs; binding is in-process for now, and remote substitution
/// swaps the Infrastructure adapter only.
/// </summary>
public interface ICollaborationResourceSummary
{
    Task<IReadOnlyDictionary<Guid, CollaborationResourceSummaryFact>> GetSummariesAsync(
        IReadOnlyCollection<(string Kind, Guid ResourceId)> resources,
        CancellationToken cancellationToken);
}
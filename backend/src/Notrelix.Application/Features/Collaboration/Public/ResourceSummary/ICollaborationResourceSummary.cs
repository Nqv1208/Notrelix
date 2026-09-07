namespace Notrelix.Application.Features.Collaboration.Public.ResourceSummary;

/// <summary>
/// Producer-owned read source for collaboration resource summaries. Consumers
/// compose this published semantic boundary into their own read models; they
/// never query Collaboration persistence directly. Resources are addressed and
/// returned by their full (kind, id) identity — the kind is part of the
/// identity, not a filter over bare ids. Binding is in-process for now, and
/// remote substitution swaps the Infrastructure adapter only.
/// </summary>
public interface ICollaborationResourceSummary
{
    Task<IReadOnlyDictionary<(string Kind, Guid ResourceId), CollaborationResourceSummaryFact>> GetSummariesAsync(
        IReadOnlyCollection<(string Kind, Guid ResourceId)> resources,
        CancellationToken cancellationToken);
}
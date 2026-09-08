namespace Notrelix.Application.Features.Collaboration.Public.ResourceSummary;

/// <summary>
/// Producer-owned per-resource collaboration summary. Collaboration owns
/// comment and attachment semantics; this fact exposes exactly the aggregated
/// counts a consumer needs as transport-neutral primitives — never Domain
/// aggregates or persistence types. The consumer maps its own canonical
/// resource identity onto <see cref="ResourceId"/>.
/// </summary>
public sealed record CollaborationResourceSummaryFact(
    Guid ResourceId,
    string ResourceKind,
    int CommentCount,
    int AttachmentCount);
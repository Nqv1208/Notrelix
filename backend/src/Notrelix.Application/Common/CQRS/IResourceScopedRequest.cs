namespace Notrelix.Application.Common.CQRS;

public interface IResourceScopedRequest
{
    string ResourceType { get; }
    Guid ResourceId { get; }
}

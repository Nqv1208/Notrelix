namespace Notrelix.Application.Common.CQRS;

public interface IResourceScopedRequest
{
    ResourceRef Resource { get; }
}

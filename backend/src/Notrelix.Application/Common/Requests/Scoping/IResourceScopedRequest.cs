namespace Notrelix.Application.Common.Requests;

public interface IResourceScopedRequest
{
    ResourceRef Resource { get; }
}

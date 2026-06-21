namespace Notrelix.Application.Common.CQRS;

public interface IActivityRequest
{
    string ActivityType { get; }
    ResourceRef Resource { get; }
}

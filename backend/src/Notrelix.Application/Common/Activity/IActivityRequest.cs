namespace Notrelix.Application.Common.Activity;

public interface IActivityRequest
{
    string ActivityType { get; }
    ResourceRef Resource { get; }
}

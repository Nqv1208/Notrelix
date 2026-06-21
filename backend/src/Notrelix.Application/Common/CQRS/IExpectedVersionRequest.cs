namespace Notrelix.Application.Common.CQRS;

public interface IExpectedVersionRequest
{
    ResourceRef Resource { get; }
    long ExpectedVersion { get; }
}

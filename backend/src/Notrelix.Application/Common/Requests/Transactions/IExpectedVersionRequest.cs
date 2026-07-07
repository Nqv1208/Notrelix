namespace Notrelix.Application.Common.Requests;

public interface IExpectedVersionRequest
{
    ResourceRef Resource { get; }
    long ExpectedVersion { get; }
}

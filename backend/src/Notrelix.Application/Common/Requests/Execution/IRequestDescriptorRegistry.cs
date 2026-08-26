namespace Notrelix.Application.Common.Requests.Execution;

public interface IRequestDescriptorRegistry
{
    RequestDescriptor GetRequired(Type requestType);

    IReadOnlyCollection<RequestDescriptor> Descriptors { get; }
}

using System.Reflection;

namespace Notrelix.Application.Common.Requests.Execution;

public sealed class RequestDescriptorRegistry : IRequestDescriptorRegistry
{
    private readonly IReadOnlyDictionary<Type, RequestDescriptor> _descriptors;

    private RequestDescriptorRegistry(IReadOnlyDictionary<Type, RequestDescriptor> descriptors)
    {
        _descriptors = descriptors;
        Descriptors = descriptors.Values.OrderBy(descriptor => descriptor.RequestType.FullName).ToArray();
    }

    public IReadOnlyCollection<RequestDescriptor> Descriptors { get; }

    public static RequestDescriptorRegistry Create(Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        var requestTypes = applicationAssembly.GetTypes()
            .Where(IsConcreteRequest)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        var descriptors = new Dictionary<Type, RequestDescriptor>();
        var violations = new List<string>();

        foreach (var requestType in requestTypes)
        {
            try
            {
                descriptors.Add(requestType, RequestDescriptorValidator.Create(requestType));
            }
            catch (SecurityMisconfigurationException exception)
            {
                violations.Add(exception.Message);
            }
        }

        if (violations.Count > 0)
        {
            throw new SecurityMisconfigurationException(
                $"Request descriptor registry contains {violations.Count} invalid contract(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, violations));
        }

        return new RequestDescriptorRegistry(descriptors);
    }

    public RequestDescriptor GetRequired(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return _descriptors.TryGetValue(requestType, out var descriptor)
            ? descriptor
            : throw new SecurityMisconfigurationException(
                $"No request descriptor is registered for {requestType.FullName}.");
    }

    private static bool IsConcreteRequest(Type type) =>
        type is { IsAbstract: false, IsInterface: false }
        && type.Namespace?.StartsWith("Notrelix.Application", StringComparison.Ordinal) == true
        && type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IRequest<>));
}

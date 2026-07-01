using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Messaging;

public sealed class CompositeIntegrationEventMapper : IIntegrationEventMapper
{
    private readonly IServiceProvider _serviceProvider;
    private IEnumerable<IIntegrationEventMapper>? _mappers;

    public CompositeIntegrationEventMapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyList<IntegrationEventMapping> Map(IDomainEvent domainEvent)
    {
        _mappers ??= _serviceProvider
            .GetServices<IIntegrationEventMapper>()
            .Where(m => m is not CompositeIntegrationEventMapper)
            .ToList();

        var results = new List<IntegrationEventMapping>();
        foreach (var mapper in _mappers)
        {
            results.AddRange(mapper.Map(domainEvent));
        }
        return results;
    }
}

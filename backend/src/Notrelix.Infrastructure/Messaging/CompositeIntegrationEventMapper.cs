using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;

namespace Notrelix.Infrastructure.Messaging;

public sealed class CompositeIntegrationEventMapper : IIntegrationEventMapper
{
    private readonly IEnumerable<IIntegrationEventMapper> _mappers;

    public CompositeIntegrationEventMapper(IEnumerable<IIntegrationEventMapper> mappers)
    {
        _mappers = mappers;
    }

    public IReadOnlyList<IntegrationEventMapping> Map(IDomainEvent domainEvent)
    {
        var results = new List<IntegrationEventMapping>();
        foreach (var mapper in _mappers)
        {
            var mapped = mapper.Map(domainEvent);
            results.AddRange(mapped);
        }
        return results;
    }
}

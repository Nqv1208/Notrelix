using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Projections.Search;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;

namespace Notrelix.Infrastructure.Tests.Data;

public class DomainEventInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityHasDomainEvent_ShouldWriteOutboxEntry()
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var now = DateTimeOffset.UtcNow;
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        var eventTypeRegistry = new Mock<IEventTypeRegistry>();
        eventTypeRegistry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var integrationEventMapper = new Mock<IIntegrationEventMapper>();
        integrationEventMapper.Setup(x => x.Map(It.IsAny<IDomainEvent>())).Returns([]);
        var classificationPolicy = new Mock<IClassificationPolicy>();
        classificationPolicy.Setup(x => x.GetClassification(It.IsAny<Type>()))
            .Returns(new Classification { Value = EventClassification.Business });
        var deliveryPolicy = new Mock<IDeliveryPolicy>();
        deliveryPolicy.Setup(x => x.GetDecision(It.IsAny<Type>()))
            .Returns(new DeliveryDecision { Outbox = true });
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        integrationEventCollector.Setup(x => x.DequeueAll()).Returns([]);
        var interceptor = new DomainEventInterceptor(
            dateTimeProvider.Object, eventTypeRegistry.Object, classificationPolicy.Object, deliveryPolicy.Object, integrationEventMapper.Object, integrationEventCollector.Object);
        await using var context = CreateContext(interceptor);

        var workspace = Workspace.Create(
            Guid.NewGuid(), Guid.CreateVersion7(), "Personal Workspace", "personal-workspace",
            DateTimeOffset.UtcNow, isPersonal: true);
        context.Workspaces.Add(workspace);

        await context.SaveChangesAsync();

        var eventLogs = context.Set<DomainEventLog>().ToList();
        eventLogs.Should().ContainSingle();
        eventLogs[0].EventName.Should().Be("test.event");
        eventLogs[0].OccurredAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    private static ApplicationDbContext CreateContext(DomainEventInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-domain-events-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new TestApplicationDbContext(options);
    }

    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }
}

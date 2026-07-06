using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Infrastructure;

namespace Notrelix.Integration.Tests.Messaging;

public class MessagingRegistrationTests
{
    [Fact]
    public void AddMessaging_WhenTransportIsNone_InDevelopment_ShouldSucceed()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "None",
                ["DOTNET_ENVIRONMENT"] = "Development"
            })
            .Build();

        var services = new ServiceCollection();

        // Should not throw
        services.AddMessaging(configuration);

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IIntegrationEventBus>();
        bus.Should().NotBeNull();
    }

    [Fact]
    public void AddMessaging_WhenTransportIsNone_InProduction_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "None",
                ["DOTNET_ENVIRONMENT"] = "Production"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddMessaging(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only allowed in Development*");
    }

    [Fact]
    public void AddMessaging_WhenTransportIsInvalid_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "InvalidTransport"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddMessaging(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown Messaging:Transport*");
    }

    [Fact]
    public void AddMessaging_WhenTransportIsRabbitMQ_ShouldSucceed()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "RabbitMQ",
                ["Messaging:RabbitMQ:Host"] = "localhost",
                ["Messaging:RabbitMQ:Username"] = "guest",
                ["Messaging:RabbitMQ:Password"] = "guest"
            })
            .Build();

        var services = new ServiceCollection();

        // Should not throw — RabbitMQ transport is now fully implemented.
        services.AddMessaging(configuration);

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IIntegrationEventBus>();
        bus.Should().NotBeNull();
    }

    [Fact]
    public void AddMessaging_WhenTransportIsKafka_ShouldThrowNotImplemented()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "Kafka"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddMessaging(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not implemented yet*");
    }

    [Fact]
    public void AddMessaging_WhenTransportNotSet_ShouldDefaultToInMemory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        services.AddMessaging(configuration);

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IIntegrationEventBus>();
        bus.Should().NotBeNull();
    }
}

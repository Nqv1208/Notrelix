using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Data;

public class OwnerDbContextProviderTests
{
    private const string Runtime = "Host=db;Database=notrelix;Username=runtime;Password=r";
    private const string Owner = "Host=db;Database=notrelix;Username=owner;Password=o";

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void ResolveConnectionString_WithoutOwnerString_FallsBackToTheRuntimeString()
    {
        var resolved = OwnerDbContextProvider.ResolveConnectionString(
            Config(("ConnectionStrings:NotrelixDb", Runtime)), out var fallback);

        resolved.Should().Be(Runtime);
        fallback.Should().BeTrue("every existing environment keeps its single connection string");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveConnectionString_WithBlankOwnerString_FallsBackToTheRuntimeString(string blank)
    {
        var resolved = OwnerDbContextProvider.ResolveConnectionString(
            Config(("ConnectionStrings:NotrelixDb", Runtime), ("ConnectionStrings:NotrelixMigrationDb", blank)),
            out var fallback);

        resolved.Should().Be(Runtime);
        fallback.Should().BeTrue();
    }

    [Fact]
    public void ResolveConnectionString_WithOwnerString_UsesItAndNotTheRuntimeString()
    {
        var resolved = OwnerDbContextProvider.ResolveConnectionString(
            Config(("ConnectionStrings:NotrelixDb", Runtime), ("ConnectionStrings:NotrelixMigrationDb", Owner)),
            out var fallback);

        resolved.Should().Be(Owner);
        fallback.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, "Username=runtime")]
    [InlineData(true, "Username=owner")]
    public void Context_ConnectsWithTheResolvedString(bool ownerConfigured, string expectedUser)
    {
        var settings = ownerConfigured
            ? Config(("ConnectionStrings:NotrelixDb", Runtime), ("ConnectionStrings:NotrelixMigrationDb", Owner))
            : Config(("ConnectionStrings:NotrelixDb", Runtime));
        using var services = new ServiceCollection()
            .AddSingleton<ICurrentTenantContext>(new FakeCurrentTenantContext())
            .AddSingleton(new AuditableEntityInterceptor(Mock.Of<ICurrentUser>(), Mock.Of<IDateTimeProvider>()))
            .AddSingleton(new DomainEventInterceptor(
                Mock.Of<IDateTimeProvider>(), Mock.Of<IEventTypeRegistry>(), Mock.Of<IClassificationPolicy>(),
                Mock.Of<IDeliveryPolicy>(), Mock.Of<IIntegrationEventMapper>(), Mock.Of<IIntegrationEventCollector>()))
            .BuildServiceProvider();

        using var provider = new OwnerDbContextProvider(
            services, settings, NullLogger<OwnerDbContextProvider>.Instance);

        var context = provider.Context;
        context.Database.GetDbConnection().ConnectionString.Should().Contain(expectedUser);
        provider.Context.Should().BeSameAs(context, "one owner context per scope");
    }
}

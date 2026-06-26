using System.Reflection;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.Resiliency;

public class ConnectionResiliencyTests
{
    [Fact]
    public void UseNpgsql_WhenValidConnectionString_RegistersNpgsqlExtension()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql("Host=localhost;Database=notrelix_test");

        var extension = optionsBuilder.Options.Extensions
            .FirstOrDefault(e => e.GetType().Name == "NpgsqlOptionsExtension");

        extension.Should().NotBeNull("UseNpgsql should register an NpgsqlOptionsExtension");
    }

    [Fact]
    public void UseNpgsql_WithRetryOnFailure_ConfiguresExecutionStrategy()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=notrelix_test",
            npgOptions => npgOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null));

        var extension = optionsBuilder.Options.Extensions
            .FirstOrDefault(e => e.GetType().Name == "NpgsqlOptionsExtension");

        extension.Should().NotBeNull();
        var factory = extension!.GetType()
            .GetProperty("ExecutionStrategyFactory", BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(extension);
        factory.Should().NotBeNull("EnableRetryOnFailure should configure an execution strategy factory");
    }

    [Fact]
    public void UseNpgsql_WithoutRetryOnFailure_ExecutionStrategyIsDefault()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql("Host=localhost;Database=notrelix_test");

        var extension = optionsBuilder.Options.Extensions
            .FirstOrDefault(e => e.GetType().Name == "NpgsqlOptionsExtension");

        extension.Should().NotBeNull();
        var factory = extension!.GetType()
            .GetProperty("ExecutionStrategyFactory", BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(extension);
        factory.Should().BeNull("without EnableRetryOnFailure, no custom ExecutionStrategyFactory should be set");
    }
}

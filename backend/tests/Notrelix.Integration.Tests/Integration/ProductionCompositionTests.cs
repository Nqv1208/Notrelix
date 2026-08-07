using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notrelix.Infrastructure;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// FZ-00 blocker #11: the production composition graph must stay closed.
/// Every MediatR request handler registered in the Application assembly must be
/// resolvable from the production container (Application + Infrastructure wiring)
/// so that a new handler can never ship without its dependencies being registered.
/// </summary>
public sealed class ProductionCompositionTests
{
    [Fact]
    public void Every_MediatR_Handler_Resolves_From_Production_Composition()
    {
        using var host = CreateHost();

        var registrations = GetHandlerRegistrations();

        registrations.Should().NotBeEmpty("the Application assembly must register request handlers");

        var failures = new List<string>();

        foreach (var registration in registrations)
        {
            try
            {
                using var scope = host.Services.CreateScope();
                var instance = scope.ServiceProvider.GetRequiredService(registration.Contract);
                instance.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                failures.Add($"{registration.Implementation.FullName}: {ex.GetBaseException().Message}");
            }
        }

        failures.Should().BeEmpty(
            "every request handler must resolve from the production composition — missing registrations are foundation defects");
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Development",
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DOTNET_ENVIRONMENT"] = "Development",
            ["Messaging:Transport"] = "None",
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["ConnectionStrings:NotrelixDb"] = "Host=localhost;Port=5432;Database=notrelix_test;Username=notrelix;Password=notrelix_test",
            ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
            ["JwtSettings:Issuer"] = "notrelix-test",
            ["JwtSettings:Audience"] = "notrelix-test",
            ["JwtSettings:ExpireMinutes"] = "30",
            ["JwtSettings:RefreshTokenExpireDays"] = "7",
        });

        builder.AddApplicationServices();
        builder.Services.AddRouting();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

        return builder.Build();
    }

    private static (Type Contract, Type Implementation)[] GetHandlerRegistrations()
    {
        var applicationAssembly = typeof(Notrelix.Application.Common.Context.ExecutionContext).Assembly;

        return applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                            || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
                .Select(i => (Contract: i, Implementation: t)))
            .OrderBy(x => x.Implementation.FullName, StringComparer.Ordinal)
            .ToArray();
    }
}

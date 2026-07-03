using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Testing.Application.Fakes;
using StackExchange.Redis;

namespace Notrelix.API.Tests.Contracts;

public class NotrelixApiFactory : WebApplicationFactory<Program>
{
    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentTenantContext? tenant)
            : base(options, tenant)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);

            // Workspace.IsDeleted is ignored in the base config and is a
            // read-only computed property (=> DeletedAt.HasValue). It can't be
            // remapped. See handler mock below for the workaround.

            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                // API contract tests do not validate reverse-proxy configuration.
                // Production/CI runtime can remain strict; test host bypasses it.
                ["ForwardedHeaders:RequireKnownProxyInProduction"] = "false",

                // Keep Redis config present for code paths that still read it.
                // Redis-backed services are replaced below with in-memory/mocks.
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",

                // JWT settings used by the API startup/config binding.
                ["JwtSettings:SecretKey"] = "ci-test-secret-key-must-be-at-least-32-characters",
                ["JwtSettings:Issuer"] = "Notrelix.Tests",
                ["JwtSettings:Audience"] = "Notrelix.Tests",
                ["JwtSettings:ExpireMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpireDays"] = "7",

                // CORS config required by startup validation.
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Cors:AllowedOrigins:1"] = "http://localhost:3000"
            };

            config.AddInMemoryCollection(testSettings);
        });

        builder.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateOnBuild = false;
            options.ValidateScopes = false;
        });

        builder.ConfigureTestServices(services =>
        {
            // Fully replace EF Core persistence: AddPersistence registers Npgsql
            // via AddDbContext, which conflicts with our InMemory replacement.
            // RemoveAll and re-AddDbContext doesn't clear EF Core's internal
            // service provider (Npgsql vs InMemory conflict). Instead, register
            // options and context directly without AddDbContext.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IApplicationDbContext>();

            services.AddSingleton(sp =>
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("Notrelix-API-Test")
                    .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                    .Options);

            services.AddScoped<ICurrentTenantContext>(_ =>
            {
                var tenant = new FakeCurrentTenantContext();
                tenant.SetWorkspace(Guid.Parse("A0000000-0000-0000-0000-000000000001"), Guid.Parse("A0000000-0000-0000-0000-000000000001"), null);
                return tenant;
            });

            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                var tenant = sp.GetRequiredService<ICurrentTenantContext>();

                return new TestApplicationDbContext(options, tenant);
            });

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            // Replace Redis cache with in-memory distributed cache for testing.
            // Remove all Redis-dependent services to prevent DI resolution failures.
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IDistributedCache>();
            services.RemoveAll<IRedisCacheService>();
            services.AddDistributedMemoryCache();

            // Redis-dependent services used by middleware/application services.
            services.RemoveAll<IRateLimitService>();
            services.AddSingleton<IRateLimitService>(_ => Mock.Of<IRateLimitService>());

            services.RemoveAll<IOtpService>();
            services.AddSingleton<IOtpService>(_ => Mock.Of<IOtpService>());

            services.RemoveAll<IJwtBlacklistService>();
            services.AddSingleton<IJwtBlacklistService>(_ => Mock.Of<IJwtBlacklistService>());

            services.RemoveAll<INotificationService>();
            services.AddScoped<INotificationService>(_ => Mock.Of<INotificationService>());

            // Clear health checks that depend on external infrastructure.
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
            });

            // WorkspaceResolutionMiddleware requires IPermissionEvaluator.
            services.RemoveAll<IPermissionEvaluator>();
            services.AddScoped<IPermissionEvaluator>(_ =>
            {
                var mock = new Mock<IPermissionEvaluator>();

                mock.Setup(x => x.EvaluateAsync(
                        It.IsAny<PermissionContext>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PermissionDecision(true));

                return mock.Object;
            });

            // Pipeline behavior dependencies.
            services.RemoveAll<IIdempotencyStore>();
            services.AddScoped<IIdempotencyStore>(_ => Mock.Of<IIdempotencyStore>());

            services.RemoveAll<IRealtimePublisher>();
            services.AddScoped<IRealtimePublisher>(_ => Mock.Of<IRealtimePublisher>());

            services.RemoveAll<IEntitlementChecker>();
            services.AddScoped<IEntitlementChecker>(_ => Mock.Of<IEntitlementChecker>());

            // CompositeIntegrationEventMapper has a circular dependency in the
            // current test host. DomainEventInterceptor only needs the abstraction.
            services.RemoveAll<IIntegrationEventMapper>();
            services.AddScoped<IIntegrationEventMapper>(_ => Mock.Of<IIntegrationEventMapper>());

            // GetUserWorkspacesQueryHandler has an untranslatable LINQ query in
            // the current test host because Workspace.IsDeleted is computed from
            // DeletedAt.HasValue and ignored in the EF model.
            services.RemoveAll<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>();
            services.AddScoped<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>();

                handler.Setup(h => h.Handle(
                        It.IsAny<GetUserWorkspacesQuery>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<List<WorkspaceDto>>.Success(new List<WorkspaceDto>()));

                return handler.Object;
            });

            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }
}
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Infrastructure.Data;

namespace Notrelix.API.Tests.Contracts;

public class NotrelixApiFactory : WebApplicationFactory<Program>
{
    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            // Workspace.IsDeleted is ignored in the base config and is a
            // read-only computed property (=> DeletedAt.HasValue). It can't be
            // remapped. See handler mock below for the workaround.
        }
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = false;
            options.ValidateScopes = false;
        });

        builder.ConfigureTestServices(services =>
        {
            // Fully replace EF Core persistence: AddPersistence registers Npgsql
            // via AddDbContext, which conflicts with our InMemory replacement.
            // RemoveAll and re-AddDbContext doesn't clear EF Core's internal
            // service provider (Npgsql vs.InMemory conflict). Instead, register
            // options and context directly without AddDbContext.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.AddSingleton(sp =>
            {
                return new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("Notrelix-API-Test")
                    .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                    .Options;
            });
            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                return new TestApplicationDbContext(options);
            });
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            // IPermissionEvaluator is not registered in the real DI (pre-existing gap).
            // WorkspaceResolutionMiddleware requires it for every request.
            services.AddScoped<IPermissionEvaluator>(_ =>
            {
                var mock = new Mock<IPermissionEvaluator>();
                mock.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PermissionDecision(true));
                return mock.Object;
            });

            // Pipeline behavior dependencies not registered in the real DI.
            // IdempotencyBehavior needs IIdempotencyStore;
            // RealtimeBehavior needs IRealtimePublisher;
            // EntitlementBehavior needs IEntitlementChecker.
            services.AddScoped<IIdempotencyStore>(_ => Mock.Of<IIdempotencyStore>());
            services.AddScoped<IRealtimePublisher>(_ => Mock.Of<IRealtimePublisher>());
            services.AddScoped<IEntitlementChecker>(_ => Mock.Of<IEntitlementChecker>());

            // CompositeIntegrationEventMapper has a circular dependency
            // (depends on IEnumerable<IIntegrationEventMapper> that includes itself).
            // Replace with a mock; DomainEventInterceptor needs IIntegrationEventMapper
            // for SaveChanges, but most queries don't trigger it.
            services.RemoveAll<IIntegrationEventMapper>();
            services.AddScoped<IIntegrationEventMapper>(_ => Mock.Of<IIntegrationEventMapper>());

            // GetUserWorkspacesQueryHandler has an untranslatable LINQ query
            // (!workspace.IsDeleted where IsDeleted is computed from DeletedAt.HasValue
            // and ignored in the EF model). Mock it to return empty results.
            services.RemoveAll<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>();
            services.AddScoped<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>>();
                handler.Setup(h => h.Handle(It.IsAny<GetUserWorkspacesQuery>(), It.IsAny<CancellationToken>()))
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

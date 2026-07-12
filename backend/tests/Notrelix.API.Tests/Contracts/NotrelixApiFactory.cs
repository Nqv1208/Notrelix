using MediatR;
using Microsoft.AspNetCore.Authentication;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Features.Identity.Auth.Commands.ForgotPassword;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Application.Features.Identity.Auth.GetBootstrap;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using InvitationByToken = Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tenancy;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
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

                // RLS config: enabled in Testing env per RlsOptionsValidator.
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",

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

            var testUserId = Guid.Parse(TestAuthHandler.TestUserId);
            var testAccountId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
            var testWorkspaceId = Guid.Parse("A0000000-0000-0000-0000-000000000001");

            services.AddScoped<ICurrentTenantContext>(_ =>
            {
                var tenant = new FakeCurrentTenantContext();
                tenant.SetWorkspace(testAccountId, testWorkspaceId, testUserId);
                return tenant;
            });

            // Mock ITenantBootstrapStore to allow access for all account/workspace operations
            services.RemoveAll<ITenantBootstrapStore>();
            services.AddScoped<ITenantBootstrapStore>(_ =>
            {
                var mock = new Mock<ITenantBootstrapStore>();
                mock.Setup(x => x.VerifyAccountAccessAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.ResolveWorkspaceAccessAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new WorkspaceAccessSnapshot(testAccountId, testWorkspaceId, testUserId, true, true));
                return mock.Object;
            });

            // Mock IAuthorizationDecisionStore (used by AuthorizationBehavior)
            services.RemoveAll<IAuthorizationDecisionStore>();
            services.AddScoped<IAuthorizationDecisionStore>(_ =>
            {
                var mock = new Mock<IAuthorizationDecisionStore>();
                mock.Setup(x => x.EvaluateAsync(
                        It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PermissionDecision(true, null));
                return mock.Object;
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
            services.AddSingleton<IRedisCacheService>(_ => Mock.Of<IRedisCacheService>());
            services.AddDistributedMemoryCache();

            // Redis-dependent services used by middleware/application services.
            services.RemoveAll<IRateLimitService>();
            services.AddSingleton<IRateLimitService>(_ => Mock.Of<IRateLimitService>());

            services.RemoveAll<IOtpService>();
            services.AddSingleton<IOtpService>(_ => Mock.Of<IOtpService>());

            services.RemoveAll<IJwtBlacklistService>();
            services.AddSingleton<IJwtBlacklistService>(_ => Mock.Of<IJwtBlacklistService>());

            // Clear health checks that depend on external infrastructure.
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
            });

            // Remove background dispatchers that use FromSqlRaw (PostgreSQL-specific)
            // since the test host uses In-Memory provider.
            services.RemoveAll<IHostedService>();

            // Remove DbRequestScopeBehavior — requires relational provider for
            // BeginTransactionAsync / ExecuteSqlRawAsync / ExecuteSqlInterpolatedAsync.
            // The In-Memory test provider does not support these.
            var dbScopeDescriptor = services.FirstOrDefault(sd =>
                !sd.IsKeyedService &&
                sd.ServiceType.IsGenericType &&
                sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
                sd.ImplementationType is { IsGenericType: true } &&
                sd.ImplementationType.GetGenericTypeDefinition() == typeof(DbRequestScopeBehavior<,>));
            if (dbScopeDescriptor is not null)
                services.Remove(dbScopeDescriptor);

            // Remove ConcurrencyBehavior — uses ResourceVersionReader which
            // calls DatabaseFacade.GetDbConnection() (relational-only). The
            // In-Memory test provider does not support this.
            var concurrencyDescriptor = services.FirstOrDefault(sd =>
                !sd.IsKeyedService &&
                sd.ServiceType.IsGenericType &&
                sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
                sd.ImplementationType is { IsGenericType: true } &&
                sd.ImplementationType.GetGenericTypeDefinition() == typeof(ConcurrencyBehavior<,>));
            if (concurrencyDescriptor is not null)
                services.Remove(concurrencyDescriptor);

            // Remove VerifiedEmailBehavior — queries IIdentityUserLookupService
            // against InMemory DB where no test user exists, causing 401.
            var verifiedEmailDescriptor = services.FirstOrDefault(sd =>
                !sd.IsKeyedService &&
                sd.ServiceType.IsGenericType &&
                sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
                sd.ImplementationType is { IsGenericType: true } &&
                sd.ImplementationType.GetGenericTypeDefinition() == typeof(VerifiedEmailBehavior<,>));
            if (verifiedEmailDescriptor is not null)
                services.Remove(verifiedEmailDescriptor);

            // Pipeline behaviors require IPermissionEvaluator.
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
            MockWorkspaceHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>(services,
                Result<List<WorkspaceDto>>.Success(new List<WorkspaceDto>()));
            // Remaining workspace and identity handlers are mocked to avoid
            // relational-DB-specific failures (transactions, RLS, FromSqlRaw)
            // that the In-Memory test provider does not support.
            MockWorkspaceHandler<GetWorkspaceQuery, Result<WorkspaceDto>>(services,
                Result<WorkspaceDto>.Success(new WorkspaceDto(
                    Guid.NewGuid(), "Mocked", "mocked", null, false, "Free", null, null, null, false, 0, DateTime.UtcNow, null)));
            MockWorkspaceHandler<UpdateWorkspaceProfileCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveWorkspaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RestoreWorkspaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>(services,
                Result<List<WorkspaceMemberDto>>.Success(new List<WorkspaceMemberDto>()));
            MockWorkspaceHandler<InviteMemberCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateMemberRoleCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RemoveMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<GetWorkspaceInvitationsQuery, Result<List<WorkspaceInvitationDto>>>(services,
                Result<List<WorkspaceInvitationDto>>.Success(new List<WorkspaceInvitationDto>()));
            MockWorkspaceHandler<GetUserPendingInvitationsQuery, Result<List<UserPendingInvitationDto>>>(services,
                Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>()));
            MockWorkspaceHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>(services,
                Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto("test-slug", Guid.NewGuid())));
            MockWorkspaceHandler<GetBootstrapQuery, Result<BootstrapResult>>(services,
                Result<BootstrapResult>.Success(CreateBootstrapResult()));
            MockWorkspaceHandler<GetCurrentUserQuery, Result<UserDto>>(services,
                Result<UserDto>.Success(CreateUserDto()));
            MockWorkspaceHandler<LogoutCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ForgotPasswordCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<LoginCommand, Result<AuthResult>>(services,
                Result<AuthResult>.Success(CreateAuthResult()));
            MockWorkspaceHandler<UpdateProfileCommand, Result<UserDto>>(services,
                Result<UserDto>.Success(CreateUserDto()));
            MockWorkspaceHandler<StartOAuthLoginCommand, Result<OAuthLoginStartResult>>(services,
                Result<OAuthLoginStartResult>.Success(new OAuthLoginStartResult("https://accounts.google.com/o/oauth2/auth?test=true")));
            MockWorkspaceHandler<CompleteOAuthLoginCommand, Result<AuthResult>>(services,
                Result<AuthResult>.Success(CreateAuthResult()));
            MockWorkspaceHandler<InvitationByToken.GetInvitationByTokenQuery, Result<InvitationByToken.WorkspaceInvitationDto>>(services,
                Result<InvitationByToken.WorkspaceInvitationDto>.Success(new InvitationByToken.WorkspaceInvitationDto(
                    Guid.NewGuid(), "Test Workspace", "Inviter", "test@test.com", "Member", false, false)));
            MockWorkspaceHandler<CreateWorkspaceCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<RegisterCommand, Result<AuthResult>>(services,
                Result<AuthResult>.Success(CreateAuthResult()));

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

    private static void MockWorkspaceHandler<TRequest, TResponse>(IServiceCollection services, TResponse result)
        where TRequest : IRequest<TResponse>
        where TResponse : class?
    {
        services.RemoveAll<IRequestHandler<TRequest, TResponse>>();
        services.AddScoped<IRequestHandler<TRequest, TResponse>>(_ =>
        {
            var handler = new Mock<IRequestHandler<TRequest, TResponse>>();
            handler.Setup(h => h.Handle(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
            return handler.Object;
        });
    }

    private static UserDto CreateUserDto() => new()
    {
        Id = Guid.NewGuid(),
        Email = "test@test.com",
        Name = "Test User"
    };

    private static AuthResult CreateAuthResult() => new()
    {
        AccessToken = "test-token",
        RefreshToken = "test-refresh-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        User = CreateUserDto()
    };

    private static BootstrapResult CreateBootstrapResult() => new()
    {
        User = CreateUserDto(),
        Workspaces = new List<WorkspaceInfo>(),
        PersonalWorkspace = new PersonalWorkspaceStatus { Status = "none" }
    };
}
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
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetAccountWorkspaces;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UpdateSpaceDescription;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceVisibility;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceType;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UnarchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RestoreSpace;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UpdateTeamDescription;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMembers;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardsBySlug;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardBySlug;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;
using Notrelix.Application.Features.WorkManagement.BoardSchema.Queries.GetBoardSchema;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Queries.GetBoardView;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;
using Notrelix.Application.Features.WorkManagement.Labels.Queries.GetLabels;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
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
            services.AddScoped<IIdempotencyStore>(_ =>
            {
                var mock = new Mock<IIdempotencyStore>();
                mock.Setup(x => x.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));
                return mock.Object;
            });

            // The scoped execution context carries the raw idempotency key.
            // Idempotent endpoints are marked with WithIdempotencyKey(), so the real
            // HttpIdempotencyEndpointFilter binds the Idempotency-Key header here.
            // CreateClient() below injects a unique test key per request; tests that
            // assert the missing/invalid-header contract must set their own headers.
            services.RemoveAll<IIdempotencyExecutionContext>();
            services.RemoveAll<IIdempotencyExecutionContextWriter>();
            services.AddScoped<IIdempotencyExecutionContext, IdempotencyExecutionContext>();
            services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
                (IIdempotencyExecutionContextWriter)sp.GetRequiredService<IIdempotencyExecutionContext>());

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
            MockWorkspaceHandler<UnarchiveWorkspaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteWorkspaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<TransferOwnershipCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<GetAccountWorkspacesQuery, Result<List<WorkspaceDto>>>(services,
                Result<List<WorkspaceDto>>.Success(new List<WorkspaceDto>()));
            MockWorkspaceHandler<ResolveSlugQuery, Result<WorkspaceDto>>(services,
                Result<WorkspaceDto>.Success(new WorkspaceDto(
                    Guid.NewGuid(), "Mocked", "mocked", null, false, "Free", null, null, null, false, 0, DateTime.UtcNow, null)));
            MockWorkspaceHandler<GetWorkspaceSettingsQuery, Result<WorkspaceSettingsDto>>(services,
                Result<WorkspaceSettingsDto>.Success(new WorkspaceSettingsDto(false, false, false, "Member", 7)));
            MockWorkspaceHandler<UpdateWorkspaceSettingsCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<AddMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<SuspendMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ActivateMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeclineInvitationCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ChangeInvitationRoleCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<CreateSpaceCommand, Result<Guid>>(services, Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<GetWorkspaceSpacesQuery, Result<List<SpaceDto>>>(services,
                Result<List<SpaceDto>>.Success(new List<SpaceDto>()));
            MockWorkspaceHandler<GetSpaceQuery, Result<SpaceDto>>(services,
                Result<SpaceDto>.Success(new SpaceDto(Guid.NewGuid(), "Mocked", null, "Workspace", "Folder", false, DateTime.UtcNow)));
            MockWorkspaceHandler<RenameSpaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UpdateSpaceDescriptionCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ChangeSpaceVisibilityCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ChangeSpaceTypeCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveSpaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UnarchiveSpaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteSpaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RestoreSpaceCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<CreateTeamCommand, Result<Guid>>(services, Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<GetWorkspaceTeamsQuery, Result<List<TeamDto>>>(services,
                Result<List<TeamDto>>.Success(new List<TeamDto>()));
            MockWorkspaceHandler<GetTeamQuery, Result<TeamDto>>(services,
                Result<TeamDto>.Success(new TeamDto(Guid.NewGuid(), "Mocked", null, false, 0, DateTime.UtcNow)));
            MockWorkspaceHandler<RenameTeamCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UpdateTeamDescriptionCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<AddTeamMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RemoveTeamMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ChangeTeamMemberRoleCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveTeamCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UnarchiveTeamCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteTeamCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RestoreTeamCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RegisterCommand, Result<AuthResult>>(services,
                Result<AuthResult>.Success(CreateAuthResult()));

            // ── WorkManagement handler mocks ──
            // Boards
            MockWorkspaceHandler<CreateBoardInWorkspaceCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateBoardCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveBoardCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UnarchiveBoardCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<AddBoardMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RemoveBoardMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<GetBoardQuery, Result<BoardDto>>(services,
                Result<BoardDto>.Success(new BoardDto(
                    Guid.NewGuid(), Guid.NewGuid(), "Test Board", null, "default",
                    "Workspace", false, 0, 0, DateTime.UtcNow)));
            MockWorkspaceHandler<GetBoardsQuery, Result<List<BoardDto>>>(services,
                Result<List<BoardDto>>.Success(new List<BoardDto>()));
            MockWorkspaceHandler<GetFullBoardQuery, Result<FullBoardDto>>(services,
                Result<FullBoardDto>.Success(new FullBoardDto(
                    Guid.NewGuid(), Guid.NewGuid(), "Test Board", null, "default",
                    "Workspace", new List<BoardFieldDto>(), new List<BoardGroupDto>(),
                    new List<BoardMemberDto>())));
            MockWorkspaceHandler<GetBoardMembersQuery, Result<List<BoardMemberDto>>>(services,
                Result<List<BoardMemberDto>>.Success(new List<BoardMemberDto>()));
            MockWorkspaceHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>(services,
                Result<List<BoardDto>>.Success(new List<BoardDto>()));
            MockWorkspaceHandler<CreateBoardBySlugCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            // Board fields
            MockWorkspaceHandler<CreateBoardFieldCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateBoardFieldCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteBoardFieldCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ReorderBoardFieldsCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<GetBoardSchemaQuery, BoardSchemaDto>(services,
                new BoardSchemaDto(Guid.NewGuid(), "Test Board", null,
                    new List<BoardFieldSchemaDto>(), new List<BoardGroupSchemaDto>()));
            // Board groups
            MockWorkspaceHandler<CreateBoardGroupCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateBoardGroupCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveBoardGroupCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UnarchiveBoardGroupCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DuplicateBoardGroupCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<ReorderBoardGroupsCommand, Result>(services, Result.Success());
            // Board items
            MockWorkspaceHandler<CreateBoardItemCommand, BoardItemSlimDto>(services,
                new BoardItemSlimDto(Guid.NewGuid(), Guid.NewGuid(), "Test Item", "a0",
                    new List<Guid>(), new List<Guid>()));
            MockWorkspaceHandler<GetBoardItemsQuery, List<BoardItemSlimDto>>(services,
                new List<BoardItemSlimDto>());
            MockWorkspaceHandler<GetBoardItemQuery, Result<BoardItemDto>>(services,
                Result<BoardItemDto>.Success(new BoardItemDto(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    "Test Item", new List<BoardItemMemberDto>(), new List<BoardItemLabelDto>(),
                    new List<ChecklistDto>(), 0, 0, "a0", DateTime.UtcNow, null)));
            MockWorkspaceHandler<UpdateBoardItemCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ArchiveBoardItemCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DuplicateBoardItemCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<MoveBoardItemCommand, BoardItemSlimDto>(services,
                new BoardItemSlimDto(Guid.NewGuid(), Guid.NewGuid(), "Test Item", "a0",
                    new List<Guid>(), new List<Guid>()));
            MockWorkspaceHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>(services,
                new BoardItemSlimDto(Guid.NewGuid(), Guid.NewGuid(), "Test Item", "a0",
                    new List<Guid>(), new List<Guid>()));
            MockWorkspaceHandler<UpdateBoardItemFieldValuesCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<AssignBoardItemMemberCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UnassignBoardItemMemberCommand, Result>(services, Result.Success());
            // Board views
            MockWorkspaceHandler<GetBoardViewQuery, Result<object>>(services,
                Result<object>.Success(new { type = "Table", config = "{}" }));
            MockWorkspaceHandler<CreateBoardViewCommand, BoardViewDto>(services,
                new BoardViewDto(Guid.NewGuid(), Guid.NewGuid(), "Test View", "Table", "{}", true));
            MockWorkspaceHandler<SaveBoardViewCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<UpdateBoardViewConfigCommand, BoardViewDto>(services,
                new BoardViewDto(Guid.NewGuid(), Guid.NewGuid(), "Test View", "Table", "{}", true));
            MockWorkspaceHandler<DeleteBoardViewCommand, Result>(services, Result.Success());
            // Labels
            MockWorkspaceHandler<CreateLabelCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<GetLabelsQuery, Result<List<BoardItemLabelDto>>>(services,
                Result<List<BoardItemLabelDto>>.Success(new List<BoardItemLabelDto>()));
            MockWorkspaceHandler<UpdateLabelCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteLabelCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<AddLabelToBoardItemCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<RemoveLabelFromBoardItemCommand, Result>(services, Result.Success());
            // Checklists
            MockWorkspaceHandler<GetChecklistsQuery, Result<List<ChecklistDto>>>(services,
                Result<List<ChecklistDto>>.Success(new List<ChecklistDto>()));
            MockWorkspaceHandler<CreateChecklistCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateChecklistCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteChecklistCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<CreateChecklistItemCommand, Result<Guid>>(services,
                Result<Guid>.Success(Guid.NewGuid()));
            MockWorkspaceHandler<UpdateChecklistItemCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<DeleteChecklistItemCommand, Result>(services, Result.Success());
            MockWorkspaceHandler<ToggleChecklistItemCommand, Result>(services, Result.Success());

            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Production registers the composite NotrelixAuth policy scheme as
            // the default authenticate/challenge scheme. The test host runs
            // after that registration, so a plain AddAuthentication override
            // does not win. PostConfigure runs after every Configure action
            // regardless of registration order, guaranteeing the test scheme.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });
    }

    /// <summary>
    /// Creates a client that automatically sends an Idempotency-Key header.
    /// Idempotent endpoints are marked with WithIdempotencyKey() and reject
    /// requests without the header; contract tests unrelated to idempotency use
    /// this default. Tests asserting the idempotency header contract itself must
    /// build a client from <see cref="WebApplicationFactory{TEntryPoint}.Server"/>
    /// and control the header explicitly.
    /// </summary>
    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return client;
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
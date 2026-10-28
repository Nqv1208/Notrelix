using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetForm;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;
using Notrelix.Domain.WorkManagement.Forms;
using NotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;
namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Forms through the real pipeline on PostgreSQL: forms and submissions resolve to their Board, a member with
/// access submits validated answers into the inbox, a principal without Board access gets NotFound, a closed
/// form refuses, and only a Board manager handles the inbox.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class FormPipelineIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private Guid _accountId, _ownerId, _memberId, _guestId, _workspaceId, _boardId, _formId;

    public FormPipelineIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        (_accountId, _ownerId, _workspaceId, _memberId) = await SeedWorkspaceStackAsync();
        _guestId = await AddMemberAsync(WorkspaceRole.Guest);
        var board = Board.Create(_accountId, _workspaceId, _ownerId, "Form Board", null, Now);
        _boardId = board.Id;
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Boards.Add(board);
            await seed.SaveChangesAsync();
        }

        _formId = (await SendAsync<Result<Guid>>(_ownerId, new CreateFormCommand(_boardId, "Yêu cầu mới"))).Data;
        await SendAsync<Result>(_ownerId, new AddFormQuestionCommand(_formId, "title", FormQuestionType.ShortText, "Tiêu đề", true, null, null));
        await SendAsync<Result>(_ownerId, new AddFormQuestionCommand(_formId, "qty", FormQuestionType.Number, "Số lượng", false, "{\"minValue\":1,\"maxValue\":10}", "a1"));
        await SendAsync<Result>(_ownerId, new PublishFormCommand(_formId));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Dictionary<string, JsonElement> Answers(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public async Task Member_SubmitsValidAnswers_IntoTheInbox_AndAManagerProcessesThem()
    {
        var id = (await SendAsync<Result<Guid>>(_memberId, new SubmitFormCommand(_formId, Answers("{\"title\":\"Mua ghế\",\"qty\":3}")))).Data;

        var inbox = (await SendAsync<Result<List<FormSubmissionDto>>>(_ownerId, new ListFormSubmissionsQuery(_formId))).Data!;
        inbox.Should().ContainSingle(s => s.Id == id && s.Status == "Accepted" && s.SubmitterUserId == _memberId && s.PayloadJson.Contains("Mua ghế"));

        var itemId = await SeedItemAsync();
        (await SendAsync<Result>(_ownerId, new ProcessFormSubmissionCommand(id, itemId))).Succeeded.Should().BeTrue();
        (await SendAsync<Result<List<FormSubmissionDto>>>(_ownerId, new ListFormSubmissionsQuery(_formId))).Data!
            .Should().ContainSingle(s => s.Id == id && s.Status == "Accepted" && s.CreatedItemId == itemId && s.ProcessedAt != null);
    }

    [Fact]
    public async Task InvalidAnswers_AreRefused_AndNothingIsStored()
    {
        var act = () => SendAsync<Result<Guid>>(_memberId, new SubmitFormCommand(_formId, Answers("{\"qty\":99}")));
        var ex = (await act.Should().ThrowAsync<AppValidation>()).Which;
        ex.Errors.Keys.Should().BeEquivalentTo("title", "qty");

        (await SendAsync<Result<List<FormSubmissionDto>>>(_ownerId, new ListFormSubmissionsQuery(_formId))).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task PrincipalWithoutBoardAccess_GetsNotFound_ForReadAndSubmit()
    {
        var read = () => SendAsync<Result<FormDetailDto>>(_guestId, new GetFormQuery(_formId));
        await read.Should().ThrowAsync<NotFound>();

        var submit = () => SendAsync<Result<Guid>>(_guestId, new SubmitFormCommand(_formId, Answers("{\"title\":\"x\"}")));
        await submit.Should().ThrowAsync<NotFound>();
    }

    [Fact]
    public async Task ClosedForm_RefusesSubmissions()
    {
        await SendAsync<Result>(_ownerId, new CloseFormCommand(_formId));

        var act = () => SendAsync<Result<Guid>>(_memberId, new SubmitFormCommand(_formId, Answers("{\"title\":\"x\"}")));
        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Form_ReadsBackWithItsQuestionsInOrder()
    {
        var form = (await SendAsync<Result<FormDetailDto>>(_memberId, new GetFormQuery(_formId))).Data!;
        form.Status.Should().Be("Published");
        form.Questions.Select(q => q.QuestionKey).Should().Equal("title", "qty");
    }

    private async Task<Guid> SeedItemAsync()
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(_accountId, _workspaceId, _boardId, "Nhóm", Notrelix.Domain.SharedKernel.Color.Create("#1666CC"), Notrelix.Domain.SharedKernel.Ordering.FractionalIndex.Initial(), _ownerId, Now);
        ctx.BoardGroups.Add(group);
        var item = Notrelix.Domain.WorkManagement.Items.BoardItem.CreateRoot(_accountId, _workspaceId, _boardId, group.Id, "Mua ghế", Notrelix.Domain.SharedKernel.Ordering.FractionalIndex.Initial(), _ownerId, Now, itemKey: "FRM-1", itemSequence: 1);
        ctx.BoardItems.Add(item);
        await ctx.SaveChangesAsync();
        return item.Id;
    }

    private async Task<T> SendAsync<T>(Guid userId, object request) where T : class
    {
        using var provider = CreateProvider(_accountId, userId);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);
        return await Send<T>(scope, request);
    }

    private async Task<Guid> AddMemberAsync(WorkspaceRole role)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"wm-{Guid.NewGuid():N}@example.com", "Other", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(user);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(_accountId, _workspaceId, user.Id, role, _ownerId, now));
        await seed.SaveChangesAsync();
        await using var grant = _db.CreateOwnerContext(SystemTenant());
        await new AccessGrantProjectionService(grant).SyncWorkspaceMemberGrantAsync(_accountId, _workspaceId, user.Id, role, now, CancellationToken.None);
        await grant.SaveChangesAsync();
        return user.Id;
    }

    private static void BindIdempotencyKey(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set($"live-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);

    private static async Task<T> Send<T>(IServiceScope scope, object request) where T : class =>
        ((await scope.ServiceProvider.GetRequiredService<ISender>().Send(request)) as T)!;

    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(CreateFormCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateOwnerContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<
            IPageAuthorizationFacts,
            PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());

        services.AddScoped<IResourceLocator, ResourceLocator>();
        // ADR-009 placement: the create handlers resolve Space placement targets
        // and the target authorizer even when the request is not placed.
        services.AddScoped<global::Notrelix.Application.Features.Workspaces.Public.SpacePlacement.ISpacePlacementTargets,
            global::Notrelix.Infrastructure.Workspaces.Spaces.SpacePlacementTargets>();
        services.AddScoped<global::Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement,
            global::Notrelix.Application.Features.WorkManagement.Boards.Services.BoardSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageSpacePlacement,
            global::Notrelix.Application.Features.Documents.Pages.Services.PageSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddScoped<Notrelix.Application.Features.Governance.Public.Visibility.IResourceVisibility, PostgresResourceVisibility>();
        services.AddScoped<Notrelix.Application.Common.Tenancy.IActorLookupService, ActorLookupService>();
        services.AddScoped<IRequestHandler<CreateFormCommand, Result<Guid>>, CreateFormCommandHandler>();
        services.AddScoped<IRequestHandler<AddFormQuestionCommand, Result>, AddFormQuestionCommandHandler>();
        services.AddScoped<IRequestHandler<PublishFormCommand, Result>, PublishFormCommandHandler>();
        services.AddScoped<IRequestHandler<CloseFormCommand, Result>, CloseFormCommandHandler>();
        services.AddScoped<IRequestHandler<SubmitFormCommand, Result<Guid>>, SubmitFormCommandHandler>();
        services.AddScoped<IRequestHandler<ProcessFormSubmissionCommand, Result>, ProcessFormSubmissionCommandHandler>();
        services.AddScoped<IRequestHandler<GetFormQuery, Result<FormDetailDto>>, GetFormQueryHandler>();
        services.AddScoped<IRequestHandler<ListFormSubmissionsQuery, Result<List<FormSubmissionDto>>>, ListFormSubmissionsQueryHandler>();
        services.AddProductionCrossContextAdapters();

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid MemberId)> SeedWorkspaceStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WM Account", $"wm-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WM Workspace", $"wm-ws-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateOwnerContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id, member.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;

public sealed record ProvisionPersonalWorkspaceCommand(
    Guid UserId,
    Guid AccountId,
    string WorkspaceName,
    Guid MessageId,
    Guid? SourceEventId,
    string SourceMessageName,
    int SourceMessageVersion,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt
) : ICommand<ProvisionPersonalWorkspaceResult>,
    ISystemInternalRequest,
    ITransactionalRequest,
    IMessageTriggeredRequest,
    IIdempotentRequest,
    ISystemOperation
{
    public string ConsumerName => ConsumerNames.PersonalWorkspaceProvisioning;
    public Guid? WorkspaceId => null;

    public string IdempotencyKey => $"account-default-workspace:{AccountId}";
    public string OperationName => "ProvisionPersonalWorkspace";
    public SystemOperationReason Reason => new("Workspaces", "Auto-provision personal workspace for new user");
    Guid ISystemOperation.CorrelationId => MessageId;
}

public sealed record ProvisionPersonalWorkspaceResult(
    Guid WorkspaceId,
    bool AlreadyExisted);

public sealed class ProvisionPersonalWorkspaceCommandHandler
    : IRequestHandler<ProvisionPersonalWorkspaceCommand, ProvisionPersonalWorkspaceResult>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IDateTimeProvider _clock;

    public ProvisionPersonalWorkspaceCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IDateTimeProvider clock)
    {
        _workspaceContext = workspaceContext;
        _clock = clock;
    }

    public async Task<ProvisionPersonalWorkspaceResult> Handle(
        ProvisionPersonalWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        var existingWorkspace = await _workspaceContext.Workspaces
            .Where(w => w.IsPersonal && w.AccountId == request.AccountId)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingWorkspace is not null)
        {
            return new ProvisionPersonalWorkspaceResult(
                existingWorkspace.Id, AlreadyExisted: true);
        }

        var slug = Slug.GenerateFromName($"{request.WorkspaceName}'s Workspace");
        var workspace = WorkspaceFactory.CreateWithOwner(
            request.AccountId,
            request.UserId,
            $"{request.WorkspaceName}'s Workspace",
            slug.Value,
            request.OccurredAt,
            isPersonal: true);

        _workspaceContext.Workspaces.Add(workspace.Workspace);
        _workspaceContext.WorkspaceMembers.Add(workspace.OwnerMember);

        return new ProvisionPersonalWorkspaceResult(
            workspace.Workspace.Id,
            AlreadyExisted: false);
    }
}

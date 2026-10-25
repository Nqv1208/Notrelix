using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>Implementation of <see cref="IMemberEmailDisclosure"/>; the rule is stated on the interface.</summary>
public sealed class MemberEmailDisclosure : IMemberEmailDisclosure
{
    private readonly IWorkspaceDbContext _context;
    private readonly IIdentityUserFacts _identity;
    private readonly ICurrentRequestContext _requestContext;

    public MemberEmailDisclosure(IWorkspaceDbContext context, IIdentityUserFacts identity, ICurrentRequestContext requestContext)
    {
        _context = context;
        _identity = identity;
        _requestContext = requestContext;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(Guid workspaceId, IReadOnlyCollection<Guid> subjectIds, CancellationToken ct)
    {
        var empty = new Dictionary<Guid, string>();
        if (subjectIds.Count == 0)
            return empty;

        var readerId = _requestContext.UserId;
        var readerIsAdministrator = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == readerId)
            .Where(WorkspaceMemberRules.ActiveAdministrator)
            .AnyAsync(ct);
        if (!readerIsAdministrator)
            return empty;

        var visibleSubjects = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && subjectIds.Contains(m.UserId))
            .Where(WorkspaceMemberRules.ActiveColleague)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        return await _identity.FindEmailsAsync(workspaceId, visibleSubjects, ct);
    }
}

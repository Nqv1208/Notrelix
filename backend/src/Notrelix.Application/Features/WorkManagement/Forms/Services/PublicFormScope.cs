using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Sharing;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Application.Features.WorkManagement.Forms.Services;

/// <summary>
/// Opens the form an anonymous visitor holds the public key of, and makes that one form's tenant the scope of
/// the rest of the request. Where the host sets database session context the database derives the form from
/// the key and binds the request to it (the per-form principal: it can read that form's definition and insert
/// its submissions, nothing else); a key that opens nothing returns null. Where it does not (in-memory hosts)
/// the same gate runs here. Every refusal is the same null, so a private, closed, archived or switched-off
/// form is indistinguishable from an unknown key.
/// </summary>
public sealed class PublicFormScope
{
    private static readonly SystemOperationReason Reason =
        new("PublicForm", "an anonymous visitor opens or submits a public form by its key; the scope is bound to that one form");

    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IWorkspaceSharingFacts _sharing;

    public PublicFormScope(
        IWorkManagementDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IWorkspaceSharingFacts sharing)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _sharing = sharing;
    }

    /// <summary>The open public form with its questions, or null when the key opens nothing.</summary>
    public async Task<Form?> OpenAsync(string publicKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicKey)) return null;

        var scoped = _rls.IsSessionContextEnabled;
        if (scoped && await _rls.EnterFormSubmitScopeAsync(publicKey, Reason, ct) is null) return null;

        // The key is the only locator; the tenant filters cannot apply before the tenant is known, and under
        // the database scope the policy already limits this read to the one bound form.
        var form = await _context.Forms.IgnoreQueryFilters().Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.PublicKey == publicKey && f.DeletedAt == null && f.Status == FormStatus.Published, ct);
        if (form is null || !form.IsPublic) return null;

        if (!scoped)
        {
            var facts = await _sharing.FindAsync(form.WorkspaceId, ct);
            if (facts is not { IsActive: true, AllowsPublicLinks: true }) return null;
        }

        _tenant.SetWorkspace(form.AccountId, form.WorkspaceId, null);
        return form;
    }

    /// <summary>
    /// The options (id and label) of the board fields the form's mapped questions fill. Under the database scope
    /// this is the one bounded read the principal has of a board; elsewhere it is the ordinary read.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<(string Id, string Name)>>> MappedOptionsAsync(Form form, CancellationToken ct)
    {
        var mapped = form.Questions.Where(q => q.BoardFieldId.HasValue).Select(q => q.BoardFieldId!.Value).Distinct().ToList();
        if (mapped.Count == 0) return new Dictionary<Guid, IReadOnlyList<(string, string)>>();

        if (_rls.IsSessionContextEnabled)
        {
            return (await _rls.ReadFormSubmitFieldOptionsAsync(ct))
                .GroupBy(o => o.FieldId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<(string, string)>)g.Select(o => (o.OptionId.ToString(), o.Name)).ToList());
        }

        return (await _context.BoardFields.AsNoTracking().Include(f => f.Options)
                .Where(f => mapped.Contains(f.Id) && f.Options.Count > 0).ToListAsync(ct))
            .ToDictionary(f => f.Id, f => (IReadOnlyList<(string, string)>)f.Options.Select(o => (o.Id.ToString(), o.Name)).ToList());
    }
}

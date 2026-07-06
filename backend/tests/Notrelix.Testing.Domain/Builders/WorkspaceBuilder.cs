using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Core;

namespace Notrelix.Testing.Domain.Builders;

public class WorkspaceBuilder
{
    private Guid _accountId = Guid.NewGuid();
    private Guid _ownerId = TestIds.NewUserId();
    private string _name = "Test Workspace";
    private string _slug = "test-workspace";
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private bool _isPersonal;

    public WorkspaceBuilder WithAccountId(Guid accountId) { _accountId = accountId; return this; }
    public WorkspaceBuilder WithOwnerId(Guid ownerId) { _ownerId = ownerId; return this; }
    public WorkspaceBuilder WithName(string name) { _name = name; return this; }
    public WorkspaceBuilder WithSlug(string slug) { _slug = slug; return this; }
    public WorkspaceBuilder WithCreatedAt(DateTimeOffset createdAt) { _createdAt = createdAt; return this; }
    public WorkspaceBuilder WithIsPersonal(bool isPersonal) { _isPersonal = isPersonal; return this; }

    public Workspace Build()
    {
        return Workspace.Create(_accountId, _ownerId, _name, _slug, _createdAt, isPersonal: _isPersonal);
    }
}

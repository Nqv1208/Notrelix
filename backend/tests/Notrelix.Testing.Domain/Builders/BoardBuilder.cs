using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Testing.Core;

namespace Notrelix.Testing.Domain.Builders;

public class BoardBuilder
{
    private Guid _workspaceId = TestIds.NewWorkspaceId();
    private Guid _createdBy = TestIds.NewUserId();
    private string _title = "Test Board";
    private string? _description;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private BoardVisibility _visibility = BoardVisibility.Workspace;
    private BoardType _type = BoardType.WorkManagement;
    private BoardFamily _family = BoardFamily.Core;
    private string? _itemKeyPrefix;
    private Guid? _spaceId;

    public BoardBuilder WithWorkspaceId(Guid workspaceId) { _workspaceId = workspaceId; return this; }
    public BoardBuilder WithCreatedBy(Guid createdBy) { _createdBy = createdBy; return this; }
    public BoardBuilder WithTitle(string title) { _title = title; return this; }
    public BoardBuilder WithDescription(string? description) { _description = description; return this; }
    public BoardBuilder WithCreatedAt(DateTimeOffset createdAt) { _createdAt = createdAt; return this; }
    public BoardBuilder WithVisibility(BoardVisibility visibility) { _visibility = visibility; return this; }
    public BoardBuilder WithSpaceId(Guid? spaceId) { _spaceId = spaceId; return this; }

    public Board Build()
    {
        return Board.Create(
            Guid.NewGuid(), _workspaceId, _createdBy, _title, _description, _createdAt,
            _visibility, _type, _family, _itemKeyPrefix, _spaceId);
    }
}

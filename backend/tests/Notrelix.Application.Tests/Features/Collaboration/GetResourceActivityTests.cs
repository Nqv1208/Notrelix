using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.Collaboration.Activity.Ports;
using Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// An Item's history is read like the Item: the query asks the pipeline for ViewBoard on that item, reads only the
/// caller's Workspace, pages with bounds, and words people from the Identity lookup, never from the stored row.
/// </summary>
public class GetResourceActivityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _item = Guid.CreateVersion7();
    private readonly Guid _actor = Guid.CreateVersion7();
    private readonly Guid _person = Guid.CreateVersion7();
    private readonly Mock<IResourceActivityReader> _reader = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly GetResourceActivityQueryHandler _handler;

    public GetResourceActivityTests()
    {
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspace);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_actor, "An", "https://a/an.png"), new ActorSnapshot(_person, "Binh", null)]);
        _handler = new GetResourceActivityQueryHandler(_reader.Object, _actors.Object, _context.Object);
    }

    [Fact]
    public void TheQuery_RequiresViewBoard_OnTheBoardItemResource()
    {
        var query = GetResourceActivityQuery.ForBoardItem(_item);

        query.Action.Should().Be(PermissionAction.ViewBoard);
        query.Resource.ResourceId.Should().Be(_item);
        query.Resource.Kind.Value.Should().Be(ItemActivityTypes.ResourceKind);
        query.Should().BeAssignableTo<IResourceScopedRequest>().And.BeAssignableTo<IRequirePermission>();
    }

    [Fact]
    public async Task Rows_AreMappedWithActorAndPersonNames_AndTheChangeDetail()
    {
        _reader.Setup(r => r.ListAsync(_workspace, ItemActivityTypes.ResourceKind, _item, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceActivityPage(
            [
                new ResourceActivityRow(Guid.CreateVersion7(), _actor, ItemActivityTypes.StatusChanged,
                    "changed the status from To do to Doing", null,
                    """{"fieldName":"Status","from":"To do","to":"Doing"}""", Now),
                new ResourceActivityRow(Guid.CreateVersion7(), _actor, ItemActivityTypes.Assigned, "assigned someone to this item", null,
                    $$"""{"personId":"{{_person}}"}""", Now.AddMinutes(-5)),
                new ResourceActivityRow(Guid.CreateVersion7(), _actor, ItemActivityTypes.Created, "created this item", "Plan", "{}", Now.AddDays(-1)),
            ], 3));

        var result = await _handler.Handle(GetResourceActivityQuery.ForBoardItem(_item), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Total.Should().Be(3);
        var status = result.Data.Data[0];
        status.ActorName.Should().Be("An");
        status.ActorAvatarUrl.Should().Be("https://a/an.png");
        status.Type.Should().Be(ItemActivityTypes.StatusChanged);
        status.Change!.From.Should().Be("To do");
        status.Change.To.Should().Be("Doing");
        status.Change.FieldName.Should().Be("Status");
        status.CreatedAt.Should().Be(Now.UtcDateTime);
        result.Data.Data[1].Change!.PersonName.Should().Be("Binh");
        result.Data.Data[2].Kind.Should().Be("created");
        result.Data.Data[2].Change.Should().BeNull();
        result.Data.Data[2].ResourceTitle.Should().Be("Plan");
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 5000, 1, GetResourceActivityQuery.MaxPageSize)]
    [InlineData(4, 25, 4, 25)]
    public async Task PagingIsBounded_BeforeTheReaderIsAsked(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        _reader.Setup(r => r.ListAsync(_workspace, ItemActivityTypes.ResourceKind, _item, expectedPage, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceActivityPage([], 0));

        var result = await _handler.Handle(GetResourceActivityQuery.ForBoardItem(_item, page, pageSize), CancellationToken.None);

        result.Data!.Page.Should().Be(expectedPage);
        result.Data.PageSize.Should().Be(expectedPageSize);
        result.Data.Data.Should().BeEmpty();
        _reader.Verify(r => r.ListAsync(_workspace, ItemActivityTypes.ResourceKind, _item, expectedPage, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
    }
}

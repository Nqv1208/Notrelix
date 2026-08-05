using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Common.Abstractions;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data.ReadPorts.Collaboration;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.ReadPorts;

[Collection("Database")]
public class WorkManagementCollaborationReadPortTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkManagementCollaborationReadPortTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private static ResourceRef BoardItemRef(Guid itemId) =>
        ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId, WorkspaceId);

    private static string JsonContent(string content) => JsonSerializer.Serialize(content);

    [Fact]
    public async Task GetCountsAsync_GroupsCommentsAndAttachments_ByItemId()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var firstItem = Guid.NewGuid();
        var secondItem = Guid.NewGuid();

        context.Comments.Add(Comment.Create(AccountId, WorkspaceId, BoardItemRef(firstItem), JsonContent("one"), AccountId, now));
        context.Comments.Add(Comment.Create(AccountId, WorkspaceId, BoardItemRef(firstItem), JsonContent("two"), AccountId, now));
        context.Comments.Add(Comment.Create(AccountId, WorkspaceId, BoardItemRef(secondItem), JsonContent("three"), AccountId, now));
        context.Attachments.Add(Attachment.Create(
            AccountId, WorkspaceId, BoardItemRef(firstItem),
            AttachmentType.Document, FileMetadata.Create("a.pdf", 10, "application/pdf"), AccountId, now));
        await context.SaveChangesAsync();

        var sut = new WorkManagementCollaborationReadPort(context);
        var counts = await sut.GetCountsAsync([firstItem, secondItem], CancellationToken.None);

        counts[firstItem].Should().Be(new WorkItemCollaborationCounts(2, 1));
        counts[secondItem].Should().Be(new WorkItemCollaborationCounts(1, 0));
    }

    [Fact]
    public async Task GetCountsAsync_ReturnsZero_ForMissingIds()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);

        var sut = new WorkManagementCollaborationReadPort(context);
        var counts = await sut.GetCountsAsync([Guid.NewGuid()], CancellationToken.None);

        counts.Values.Single().Should().Be(new WorkItemCollaborationCounts(0, 0));
    }

    [Fact]
    public async Task GetCountsAsync_ExcludesSoftDeletedComments()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var item = Guid.NewGuid();
        var active = Comment.Create(AccountId, WorkspaceId, BoardItemRef(item), JsonContent("active"), AccountId, now);
        var deleted = Comment.Create(AccountId, WorkspaceId, BoardItemRef(item), JsonContent("deleted"), AccountId, now);
        deleted.Delete(AccountId, now, "cleanup");
        context.Comments.Add(active);
        context.Comments.Add(deleted);
        await context.SaveChangesAsync();

        var sut = new WorkManagementCollaborationReadPort(context);
        var counts = await sut.GetCountsAsync([item], CancellationToken.None);

        counts[item].CommentCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCountsAsync_IgnoresCommentsOnOtherResourceKinds()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var now = DateTimeOffset.UtcNow;

        var item = Guid.NewGuid();
        context.Comments.Add(Comment.Create(
            AccountId, WorkspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board"), item, WorkspaceId),
            JsonContent("board comment"), AccountId, now));
        await context.SaveChangesAsync();

        var sut = new WorkManagementCollaborationReadPort(context);
        var counts = await sut.GetCountsAsync([item], CancellationToken.None);

        counts[item].CommentCount.Should().Be(0);
    }
}

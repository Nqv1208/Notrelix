using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetForm;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// NRX-004 on PostgreSQL through the real pipeline: a person of the same Account with no Workspace membership
/// gets the identical NotFound for a resource that exists in the Workspace and for an id that never existed, so
/// the answer cannot be used to find out what exists. Forbidden stays only for a member who can see the
/// Workspace but lacks the permission (a Guest reading the member list).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class NonMemberAccessIntegrationTests : IAsyncLifetime
{
    private readonly OrgTreeTestHost _host;
    private readonly PostgresTestContainer _db;

    public NonMemberAccessIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SameAccountNonMember_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);
        var board = await _host.SeedBoardAsync(stack, "Plan");
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Task");
        var form = Form.Create(stack.AccountId, stack.WorkspaceId, board, "Intake", $"intake-{Guid.NewGuid():N}"[..16], stack.OwnerId, OrgTreeTestHost.BaseTime);
        var comment = Comment.Create(
            stack.AccountId, stack.WorkspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item, stack.WorkspaceId),
            "A comment", stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Forms.Add(form); c.Comments.Add(comment); return Task.CompletedTask; });

        var probes = new (string Name, Func<Guid, object> Existing)[]
        {
            ("board item", id => new GetBoardItemQuery(id)),
            ("form", id => new GetFormQuery(id)),
            ("comments of an item", id => new GetCommentsQuery(ResourceKind.Create("work-management.board-item"), id)),
            ("workspace members", id => new GetWorkspaceMembersQuery(id)),
        };
        var existingIds = new[] { item, form.Id, item, stack.WorkspaceId };

        using var provider = _host.CreateProvider(stack.AccountId, outsider);
        for (var i = 0; i < probes.Length; i++)
        {
            var (name, make) = probes[i];
            var found = await ThrownAsync(provider, make(existingIds[i]));
            var made = await ThrownAsync(provider, make(Guid.NewGuid()));

            made.Should().BeOfType<AppNotFound>($"{name}: a made-up id is NotFound");
            found.Should().BeOfType<AppNotFound>($"{name}: an existing id answers like a made-up one for a non-member");
        }
    }

    [Fact]
    public async Task GuestWhoSeesTheWorkspace_StillGetsForbidden_ForTheMemberList()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        using var provider = _host.CreateProvider(stack.AccountId, guest);
        var thrown = await ThrownAsync(provider, new GetWorkspaceMembersQuery(stack.WorkspaceId));

        thrown.Should().BeOfType<AppForbidden>("a Guest is a member who lacks the permission, not a stranger");
    }

    private static async Task<Exception?> ThrownAsync(ServiceProvider provider, object request)
    {
        try
        {
            using var scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<MediatR.ISender>().Send(request);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}

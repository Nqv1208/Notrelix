using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppPrecondition = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// A member's own columns of a board view (the "Cột" menu, UC-WM-I02) on PostgreSQL through the real pipeline
/// (NRX-003, NRX-004): any reader of the board saves their own columns, a reader sees only their own, a view of
/// another workspace answers like a missing one, and a viewer cannot save the shared view
/// (`BoardAuthorizationHardeningIntegrationTests.AViewer_CannotSaveTheBoardsSharedView`).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardColumnsPipelineIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";
    private const string HideDue = "{\"hidden\":[\"due\"]}";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardColumnsPipelineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AMember_SavesTheirOwnColumns_ANewSaveReplacesThem_AndAnotherMemberKeepsTheirs()
    {
        var scene = await SeedAsync();
        var colleague = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Colleague");
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);
        using var other = _host.CreateProvider(scene.Stack.AccountId, colleague);

        await OrgTreeTestHost.SendAsync(owner, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, scene.ViewId, null, HideDue));
        (await ColumnsOfAsync(scene.ViewId, scene.Stack.OwnerId)).Should().Be(HideDue);

        const string hideTags = "{\"hidden\":[\"tags\"]}";
        await OrgTreeTestHost.SendAsync(owner, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, scene.ViewId, null, hideTags));
        (await ColumnsOfAsync(scene.ViewId, scene.Stack.OwnerId)).Should().Be(hideTags, "the second save updates the same row");
        (await CountAsync(scene.ViewId, scene.Stack.OwnerId)).Should().Be(1);

        await OrgTreeTestHost.SendAsync(other, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, scene.ViewId, null, HideDue));
        (await ColumnsOfAsync(scene.ViewId, scene.Stack.OwnerId)).Should().Be(hideTags, "a colleague's columns are theirs");
        (await ColumnsOfAsync(scene.ViewId, colleague)).Should().Be(HideDue);
    }

    [Fact]
    public async Task AViewerOfTheBoard_SavesTheirOwnColumns_ButCannotSaveTheSharedView()
    {
        var scene = await SeedAsync();
        var viewer = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Guest, "Viewer");
        await _host.SeedResourcePermissionAsync(scene.Stack, BoardKind, scene.BoardId, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, viewer);

        await OrgTreeTestHost.SendAsync(provider, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, scene.ViewId, null, HideDue));
        var shared = () => OrgTreeTestHost.SendIdempotentAsync(provider, new SaveBoardViewCommand(scene.BoardId, ViewMode.Table, "{\"hidden\":[\"due\"]}"));

        (await ColumnsOfAsync(scene.ViewId, viewer)).Should().Be(HideDue, "a reader's own columns concern only them");
        await shared.Should().ThrowAsync<AppForbidden>("the shared view is changed for everyone");
    }

    [Fact]
    public async Task AViewOfAnotherWorkspace_AnswersLikeAMissingView_AndNothingIsSaved()
    {
        var scene = await SeedAsync();
        var stranger = await _host.SeedWorkspaceAsync();
        using var strangerProvider = _host.CreateProvider(stranger.AccountId, stranger.OwnerId);
        using var missingProvider = _host.CreateProvider(stranger.AccountId, stranger.OwnerId);

        var foreign = () => OrgTreeTestHost.SendAsync(strangerProvider, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, scene.ViewId, null, HideDue));
        var missing = () => OrgTreeTestHost.SendAsync(missingProvider, new CreateOrUpdateBoardPreferenceCommand(scene.BoardId, Guid.CreateVersion7(), null, HideDue));

        (await foreign.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());
        (await CountAsync(scene.ViewId, stranger.OwnerId)).Should().Be(0);
    }

    [Fact]
    public async Task SavingTheSharedView_NamesTheVersionRead_AStaleSaveChangesNothing()
    {
        var scene = await SeedAsync();
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);
        var read = await _host.ReadAsync(c => c.BoardViews.AsNoTracking().Where(v => v.Id == scene.ViewId).Select(v => v.Version).SingleAsync());

        await OrgTreeTestHost.SendIdempotentAsync(owner, new SaveBoardViewCommand(scene.BoardId, ViewMode.Table, "{\"hidden\":[\"due\"]}", read));
        var saved = await _host.ReadAsync(c => c.BoardViews.AsNoTracking().Where(v => v.Id == scene.ViewId).Select(v => v.Version).SingleAsync());
        saved.Should().BeGreaterThan(read);

        var stale = () => OrgTreeTestHost.SendIdempotentAsync(owner, new SaveBoardViewCommand(scene.BoardId, ViewMode.Table, "{\"hidden\":[\"tags\"]}", read));
        var unnamed = () => OrgTreeTestHost.SendIdempotentAsync(owner, new SaveBoardViewCommand(scene.BoardId, ViewMode.Table, "{\"hidden\":[\"tags\"]}"));

        await stale.Should().ThrowAsync<AppPrecondition>("another save went first");
        await unnamed.Should().ThrowAsync<AppPrecondition>("overwriting the shared view needs the version read");
        (await _host.ReadAsync(c => c.BoardViews.AsNoTracking().Where(v => v.Id == scene.ViewId).Select(v => v.Version).SingleAsync()))
            .Should().Be(saved, "a refused save bumps nothing");
    }

    private sealed record Scene(OrgStack Stack, Guid BoardId, Guid ViewId);

    /// <summary>A board with its shared view, saved by the owner as the first save of a view does.</summary>
    private async Task<Scene> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Columns");
        using (var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId))
            await OrgTreeTestHost.SendIdempotentAsync(owner, new SaveBoardViewCommand(board, ViewMode.Table, "{}"));
        var viewId = await _host.ReadAsync(c => c.BoardViews.AsNoTracking().Where(v => v.BoardId == board).Select(v => v.Id).SingleAsync());
        return new Scene(stack, board, viewId);
    }

    private Task<string?> ColumnsOfAsync(Guid viewId, Guid userId) =>
        _host.ReadAsync(c => c.BoardViewUserPreferences.AsNoTracking()
            .Where(p => p.ViewId == viewId && p.UserId == userId).Select(p => p.Columns).SingleAsync());

    private Task<int> CountAsync(Guid viewId, Guid userId) =>
        _host.ReadAsync(c => c.BoardViewUserPreferences.AsNoTracking().CountAsync(p => p.ViewId == viewId && p.UserId == userId));
}

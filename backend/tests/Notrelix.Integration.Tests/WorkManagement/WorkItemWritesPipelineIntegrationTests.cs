using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListArchivedBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using Conflict = Notrelix.Application.Common.Exceptions.ConflictException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// The single-item writes of the table and the card on PostgreSQL through the real pipeline (NRX-003, NRX-004,
/// NRX-007, NRX-010): rename (UC-WM-B04), assign and unassign (UC-WM-B05), status (UC-WM-D05), start and due
/// dates (UC-WM-D04), a Select value such as priority (UC-WM-D06), delete (UC-WM-B11), restore (UC-WM-B12), the
/// board Trash (UC-WM-B13) and the archived list (UC-WM-B10). Each write refuses a stale version and changes
/// nothing, a retry under one idempotency key has one effect, a semantic no-op bumps no version, and a person who
/// cannot read the board gets the answer a missing item gets.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkItemWritesPipelineIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkItemWritesPipelineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ---- UC-WM-B04 rename ----------------------------------------------------------------------------------

    [Fact]
    public async Task Rename_WithTheReadVersion_BumpsOnce_TheSameTitleChangesNothing_AndAStaleRetryIsRefused()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);

        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Renamed", read));
        var afterRename = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Renamed", afterRename));
        var afterSameTitle = await VersionAsync(scene.Items[0]);
        var staleRetry = () => OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Renamed again", read));

        afterRename.Should().Be(read + 1);
        afterSameTitle.Should().Be(afterRename, "the same title is a semantic no-op");
        await staleRetry.Should().ThrowAsync<PreconditionFailed>("the writer last read the item before the rename");
        (await ItemAsync(scene.Items[0])).Name.Should().Be("Renamed");
        (await VersionAsync(scene.Items[0])).Should().Be(afterRename);
    }

    [Fact]
    public async Task Rename_RetriedWithTheSameKeyAfterTheCommit_ReplaysTheFirstResult_AndChangesNothing()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var key = $"rename-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Renamed", read), key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        // The answer was lost: the retry names the version the first attempt read, which has moved on.
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Renamed", read), key);

        afterFirst.Should().Be(read + 1);
        replay.Should().BeEquivalentTo(first, "a replay returns the first committed result, not a stale refusal");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay wrote nothing");
        (await ItemAsync(scene.Items[0])).Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Rename_RetriedWithTheSameKeyButAnotherPayload_IsRefusedAndChangesNothing()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var key = $"rename-{Guid.NewGuid():N}";
        await OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Renamed", read), key);
        var afterFirst = await VersionAsync(scene.Items[0]);

        var mismatch = () => OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Another title", read), key);

        await mismatch.Should().ThrowAsync<IdempotencyPayloadMismatchException>();
        (await ItemAsync(scene.Items[0])).Name.Should().Be("Renamed");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst);
    }

    [Fact]
    public async Task Description_RetriedWithTheSameKeyAfterTheCommit_ReplaysTheFirstResult_AndChangesNothing()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var key = $"describe-{Guid.NewGuid():N}";
        var describe = new UpdateBoardItemCommand(scene.Items[0], null, null, null, read, "What and why");

        await OrgTreeTestHost.SendIdempotentAsync(owner, describe, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendIdempotentAsync(owner, describe, key);

        (await ItemAsync(scene.Items[0])).DescriptionMd.Should().Be("What and why");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay wrote nothing");
    }

    [Fact]
    public async Task Rename_WithANewKeyAndTheMovedVersion_IsStillRefusedAsStale()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Renamed", read), $"rename-{Guid.NewGuid():N}");

        var stale = () => OrgTreeTestHost.SendIdempotentAsync(owner, Rename(scene.Items[0], "Renamed again", read), $"rename-{Guid.NewGuid():N}");

        await stale.Should().ThrowAsync<PreconditionFailed>("a key never lets a different edit past the version check");
        (await ItemAsync(scene.Items[0])).Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Rename_ALongTitleToAShorterOne_IsAccepted_AndOneOverTheDomainLimitIsRefusedUnchanged()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var long300 = new string('a', 300);
        var read = await VersionAsync(scene.Items[0]);

        // Create takes up to the Domain limit, so rename must take the same titles.
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], long300, read));
        var afterLong = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], new string('b', 250), afterLong));
        var afterShorter = await VersionAsync(scene.Items[0]);
        var tooLong = () => OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], new string('c', BoardItem.NameMaxLength + 1), afterShorter));

        (await ItemAsync(scene.Items[0])).Name.Should().Be(new string('b', 250));
        // This host has no request validators, so the Domain guard is what answers; the API answers both with a 400.
        await tooLong.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>("a title over the Domain limit is refused");
        (await ItemAsync(scene.Items[0])).Name.Should().Be(new string('b', 250));
        (await VersionAsync(scene.Items[0])).Should().Be(afterShorter, "the refusal changed nothing");
    }

    [Fact]
    public async Task Rename_ByAMemberWhoCannotReadThePrivateBoard_AnswersLikeAMissingItem()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(scene.Items[0]);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, Rename(scene.Items[0], "Nope", version));
        var missing = () => OrgTreeTestHost.SendAsync(provider, Rename(Guid.CreateVersion7(), "Nope", version));

        var hiddenError = (await hidden.Should().ThrowAsync<AppNotFound>()).Which;
        var missingError = (await missing.Should().ThrowAsync<AppNotFound>()).Which;
        hiddenError.GetType().Should().Be(missingError.GetType());
        (await ItemAsync(scene.Items[0])).Name.Should().Be("Task 0");
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    [Fact]
    public async Task Rename_ByAPersonOfAnotherWorkspaceOrNoWorkspace_NeverReachesTheItem()
    {
        var scene = await SeedAsync();
        var version = await VersionAsync(scene.Items[0]);

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider, Rename(scene.Items[0], "Hijacked", version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await ItemAsync(scene.Items[0])).Name.Should().Be("Task 0");
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- UC-WM-D05 status ----------------------------------------------------------------------------------

    [Fact]
    public async Task Status_RetriedWithTheSameKey_HasOneEffect_AndASecondChoiceOfTheSameStatusIsANoOp()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var command = new UpdateBoardItemStatusCommand(scene.Items[0], scene.DoneOptionId.ToString(), read);
        var key = $"status-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var sameStatusAgain = await OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemStatusCommand(scene.Items[0], scene.DoneOptionId.ToString(), afterFirst));

        replay.Should().BeEquivalentTo(first, "a replay returns the first committed result");
        afterFirst.Should().BeGreaterThan(read);
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "neither the replay nor the same status again wrote anything");
        sameStatusAgain.Should().NotBeNull();
        (await ItemAsync(scene.Items[0])).CompletedAt.Should().NotBeNull("a Done-category status completes the item");
    }

    [Fact]
    public async Task Status_WithAStaleVersion_IsRefusedAndTheItemKeepsItsStatus()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Moved on", read));
        var current = await VersionAsync(scene.Items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemStatusCommand(scene.Items[0], scene.DoneOptionId.ToString(), read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[0])).CompletedAt.Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(current);
    }

    [Fact]
    public async Task Status_OnAPrivateBoardTheMemberCannotRead_AnswersLikeAMissingItem()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(scene.Items[0]);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider,
            new UpdateBoardItemStatusCommand(scene.Items[0], scene.DoneOptionId.ToString(), version));
        var missing = () => OrgTreeTestHost.SendAsync(provider,
            new UpdateBoardItemStatusCommand(Guid.CreateVersion7(), scene.DoneOptionId.ToString(), version));

        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    [Fact]
    public async Task Status_ByAPersonOutsideTheWorkspace_NeverReachesTheItem()
    {
        var scene = await SeedAsync();
        var version = await VersionAsync(scene.Items[0]);

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider,
                new UpdateBoardItemStatusCommand(scene.Items[0], scene.DoneOptionId.ToString(), version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await ItemAsync(scene.Items[0])).CompletedAt.Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- UC-WM-D04 dates -----------------------------------------------------------------------------------

    [Fact]
    public async Task DueDate_RetriedWithTheSameKey_HasOneEffect_AndTheSameDateChangesNothing()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var due = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);
        var command = new SetBoardItemDueDateCommand(scene.Items[0], due, null, read);
        var key = $"due-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        await OrgTreeTestHost.SendAsync(owner, new SetBoardItemDueDateCommand(scene.Items[0], due, null, afterFirst));

        replay.Should().BeEquivalentTo(first);
        afterFirst.Should().Be(read + 1);
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay and the same date again wrote nothing");
        (await ItemAsync(scene.Items[0])).DueAt.Should().Be(new DateTimeOffset(due));
    }

    [Fact]
    public async Task DueDate_WithAStaleVersion_IsRefusedAndTheDateStaysUnset()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Moved on", read));
        var current = await VersionAsync(scene.Items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner,
            new SetBoardItemDueDateCommand(scene.Items[0], new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc), null, read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[0])).DueAt.Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(current);
    }

    [Fact]
    public async Task DueDate_ByAPersonOutsideTheWorkspace_NeverReachesTheItem()
    {
        var scene = await SeedAsync();
        var version = await VersionAsync(scene.Items[0]);

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider,
                new SetBoardItemDueDateCommand(scene.Items[0], new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc), null, version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await ItemAsync(scene.Items[0])).DueAt.Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- UC-WM-D04 dates through the item update the table uses (PATCH /board-items/{id}) ------------------

    [Fact]
    public async Task DueDateThroughTheItemUpdate_SetsTheDay_ClearsIt_AndTheSameDayAgainBumpsNoVersion()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var due = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var read = await VersionAsync(scene.Items[0]);

        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemCommand(scene.Items[0], null, due, null, read));
        var afterSet = await ItemAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemCommand(scene.Items[0], null, due, null, afterSet.Version));
        var afterSame = await ItemAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemCommand(scene.Items[0], null, null, null, afterSame.Version, ClearDueDate: true));

        afterSet.DueAt.Should().Be(new DateTimeOffset(due));
        afterSet.Version.Should().Be(read + 1);
        afterSame.Version.Should().Be(afterSet.Version, "the same day is a semantic no-op");
        (await ItemAsync(scene.Items[0])).DueAt.Should().BeNull();
    }

    [Fact]
    public async Task DueDateThroughTheItemUpdate_BeforeTheStartDate_IsRefusedAndNothingChanges()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var start = new DateTime(2026, 12, 10, 0, 0, 0, DateTimeKind.Utc);
        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemCommand(scene.Items[0], null, null, start, await VersionAsync(scene.Items[0])));
        var before = await ItemAsync(scene.Items[0]);

        var onlyDue = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemCommand(scene.Items[0], null, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), null, before.Version));
        var both = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemCommand(scene.Items[0], null, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc), before.Version));

        (await onlyDue.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.WorkManagement.WorkManagementRuleCodes.WorkManagement_Item_DueDateMustBeAfterStartDate);
        await both.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        var after = await ItemAsync(scene.Items[0]);
        after.DueAt.Should().BeNull();
        after.StartedAt.Should().Be(before.StartedAt);
        after.Version.Should().Be(before.Version, "a refused range changes nothing");
    }

    [Fact]
    public async Task DueDateThroughTheItemUpdate_WithAStaleVersion_IsRefusedAndTheDateStaysUnset()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Moved on", read));
        var current = await VersionAsync(scene.Items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemCommand(scene.Items[0], null, new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc), null, read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[0])).DueAt.Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(current);
    }

    // ---- UC-WM-B09 archive ---------------------------------------------------------------------------------

    [Fact]
    public async Task Archive_RetriedWithTheSameKey_HasOneEffect_AndAStaleVersionIsRefusedAndChangesNothing()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var command = new ArchiveBoardItemCommand(scene.Items[0], read);
        var key = $"archive-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await ItemAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var secondRead = await VersionAsync(scene.Items[1]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[1], "Moved on", secondRead));
        var stale = () => OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(scene.Items[1], secondRead));

        replay.Should().BeEquivalentTo(first, "a replay returns the first committed result");
        afterFirst.IsArchived.Should().BeTrue();
        afterFirst.Version.Should().Be(read + 1, "one logical archive raised the version once");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst.Version, "the replay archived nothing a second time");
        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[1])).IsArchived.Should().BeFalse("a refused archive changes nothing");
    }

    // ---- UC-WM-J02, E04, E06, F03 move between groups or lanes ----------------------------------------------

    [Fact]
    public async Task Move_RetriedWithTheSameKey_HasOneEffect_AndAStaleMoveIsRefusedAndChangesNothing()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var move = new MoveBoardItemCommand(scene.Items[0], scene.OtherGroupId, read);
        var key = $"move-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, move, key);
        var afterFirst = await ItemAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, move, key);
        var secondRead = await VersionAsync(scene.Items[1]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[1], "Moved on", secondRead));
        var stale = () => OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(scene.Items[1], scene.OtherGroupId, secondRead));

        replay.Should().BeEquivalentTo(first, "a replay returns the first committed result");
        afterFirst.GroupId.Should().Be(scene.OtherGroupId);
        afterFirst.Version.Should().Be(read + 1, "one logical move raised the version once");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst.Version, "the replay moved nothing a second time");
        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[1])).GroupId.Should().NotBe(scene.OtherGroupId, "a refused move changes nothing");
    }

    [Fact]
    public async Task Move_ByAMemberWhoCannotReadThePrivateBoard_AnswersLikeAMissingItem_AndAnOutsiderNeverReachesIt()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(scene.Items[0]);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, new MoveBoardItemCommand(scene.Items[0], scene.OtherGroupId, version));
        var missing = () => OrgTreeTestHost.SendAsync(provider, new MoveBoardItemCommand(Guid.CreateVersion7(), scene.OtherGroupId, version));
        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());

        await ForEachOutsiderAsync(scene.Stack, async outsider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(outsider, new MoveBoardItemCommand(scene.Items[0], scene.OtherGroupId, version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await ItemAsync(scene.Items[0])).GroupId.Should().NotBe(scene.OtherGroupId);
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- a card dropped in a Kanban lane: placement and lane value in one command ---------------------------

    [Fact]
    public async Task MoveToLane_WritesGroupPositionAndTheLaneValueTogether_AStatusLaneAlsoCompletesTheItem()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);

        await OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, read, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId));
        var priority = await ItemAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[1], scene.OtherGroupId, await VersionAsync(scene.Items[1]), PreviousSiblingId: scene.Items[0],
            LaneFieldId: scene.StatusFieldId, LaneOptionId: scene.DoneOptionId));
        var done = await ItemAsync(scene.Items[1]);

        priority.GroupId.Should().Be(scene.OtherGroupId);
        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().Contain(scene.HighOptionId.ToString());
        done.GroupId.Should().Be(scene.OtherGroupId);
        done.Position.Should().BeGreaterThan(priority.Position, "the card sits after the sibling it named");
        done.CompletedAt.Should().NotBeNull("a Done-category lane completes the item, as the status write does");
        (await StoredValueAsync(scene.Items[1], scene.StatusFieldId)).Should().Contain(scene.DoneOptionId.ToString());
    }

    [Fact]
    public async Task MoveToLane_WithAStaleVersion_IsRefusedAndNeitherThePlaceNorTheLaneValueChanges()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Moved on", read));
        var current = await ItemAsync(scene.Items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, read, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        var after = await ItemAsync(scene.Items[0]);
        after.Version.Should().Be(current.Version, "a refused drop is not partly kept");
        after.GroupId.Should().Be(current.GroupId);
        after.Position.Should().Be(current.Position);
        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().BeNull();
    }

    [Fact]
    public async Task MoveToLane_WithAnOptionTheFieldDoesNotHave_IsRefusedAndTheItemDoesNotMove()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);

        var refused = () => OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, read, LaneFieldId: scene.PriorityFieldId, LaneOptionId: Guid.CreateVersion7()));

        await refused.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        var after = await ItemAsync(scene.Items[0]);
        after.GroupId.Should().NotBe(scene.OtherGroupId, "the place is not kept when the lane value is refused");
        after.Version.Should().Be(read);
    }

    [Fact]
    public async Task MoveToLane_RetriedWithTheSameKey_HasOneEffect_AndAnotherPayloadUnderTheKeyIsRefused()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var command = new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, read, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId);
        var key = $"lane-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var otherPayload = () => OrgTreeTestHost.SendIdempotentAsync(owner, command with { LaneOptionId = scene.DoneOptionId }, key);

        replay.Should().BeEquivalentTo(first, "a replay returns the first committed result, not a stale refusal");
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay wrote nothing");
        await otherPayload.Should().ThrowAsync<IdempotencyPayloadMismatchException>();
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst);
    }

    [Fact]
    public async Task MoveToLane_ToThePlaceAndValueTheItemHolds_BumpsNoVersion()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        await OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, await VersionAsync(scene.Items[0]), LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId));
        var settled = await VersionAsync(scene.Items[0]);

        await OrgTreeTestHost.SendAsync(owner, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, settled, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId));

        (await VersionAsync(scene.Items[0])).Should().Be(settled, "the same lane and the same place is a semantic no-op");
    }

    [Fact]
    public async Task MoveToLane_ByAViewer_IsForbiddenAndNothingChanges()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var viewer = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Viewer");
        await _host.SeedResourcePermissionAsync(scene.Stack, "work-management.board", scene.BoardId, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);
        var version = await VersionAsync(scene.Items[0]);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, viewer);

        var act = () => OrgTreeTestHost.SendAsync(provider, new MoveBoardItemCommand(
            scene.Items[0], scene.OtherGroupId, version, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId));

        await act.Should().ThrowAsync<AppForbidden>();
        (await ItemAsync(scene.Items[0])).GroupId.Should().NotBe(scene.OtherGroupId);
        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    [Fact]
    public async Task MoveToLane_ByAMemberWhoCannotReadTheBoardOrAnOutsider_NeverReachesTheItem()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(scene.Items[0]);
        MoveBoardItemCommand Lane(Guid id) => new(id, scene.OtherGroupId, version, LaneFieldId: scene.PriorityFieldId, LaneOptionId: scene.HighOptionId);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, Lane(scene.Items[0]));
        var missing = () => OrgTreeTestHost.SendAsync(provider, Lane(Guid.CreateVersion7()));
        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());
        await ForEachOutsiderAsync(scene.Stack, async outsider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(outsider, Lane(scene.Items[0]));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- UC-WM-B05 assignees -------------------------------------------------------------------------------

    [Fact]
    public async Task Assign_RetriedWithTheSameKey_HasOneEffect_AndAssigningAgainChangesNothing()
    {
        var scene = await SeedAsync();
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Assignee");
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var command = new AssignBoardItemMemberCommand(scene.Items[0], member, read);
        var key = $"assign-{Guid.NewGuid():N}";

        await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        await OrgTreeTestHost.SendAsync(owner, new AssignBoardItemMemberCommand(scene.Items[0], member, afterFirst));

        (await AssigneesAsync(scene.Items[0])).Should().Equal(member);
        afterFirst.Should().Be(read + 1);
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay and the second assign wrote nothing");
    }

    [Fact]
    public async Task AssignAndUnassign_WithAStaleVersion_AreRefusedAndTheAssigneesStay()
    {
        var scene = await SeedAsync();
        var first = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "First");
        var second = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Second");
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, new AssignBoardItemMemberCommand(scene.Items[0], first, read));
        var current = await VersionAsync(scene.Items[0]);

        var staleAssign = () => OrgTreeTestHost.SendAsync(owner, new AssignBoardItemMemberCommand(scene.Items[0], second, read));
        var staleUnassign = () => OrgTreeTestHost.SendAsync(owner, new UnassignBoardItemMemberCommand(scene.Items[0], first, read));

        await staleAssign.Should().ThrowAsync<PreconditionFailed>();
        await staleUnassign.Should().ThrowAsync<PreconditionFailed>();
        (await AssigneesAsync(scene.Items[0])).Should().Equal(first);
        (await VersionAsync(scene.Items[0])).Should().Be(current);
    }

    [Fact]
    public async Task Assign_OfSomeoneOfAnotherWorkspaceOrWhoCannotSeeTheBoard_IsRefusedWithTheReasonAndChangesNothing()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var cannotSee = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Cannot see");
        var otherWorkspace = await _host.SeedWorkspaceAsync();
        using var owner = Provider(scene.Stack);
        var version = await VersionAsync(scene.Items[0]);

        var notShared = () => OrgTreeTestHost.SendAsync(owner, new AssignBoardItemMemberCommand(scene.Items[0], cannotSee, version));
        var foreign = () => OrgTreeTestHost.SendAsync(owner, new AssignBoardItemMemberCommand(scene.Items[0], otherWorkspace.OwnerId, version));

        (await notShared.Should().ThrowAsync<AppForbidden>("the person cannot see the Private board, so the owner must share it first"))
            .Which.Code.Should().Be(BoardAssigneeGuard.BoardNotSharedCode, "the page names the reason and offers the share action from this code");
        await foreign.Should().ThrowAsync<AppForbidden>("only people of the same Workspace can be assigned");
        (await AssigneesAsync(scene.Items[0])).Should().BeEmpty();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    [Fact]
    public async Task Assign_ByAPersonOutsideTheWorkspace_NeverReachesTheItem()
    {
        var scene = await SeedAsync();
        var assignee = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Assignee");
        var version = await VersionAsync(scene.Items[0]);

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider,
                new AssignBoardItemMemberCommand(scene.Items[0], assignee, version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await AssigneesAsync(scene.Items[0])).Should().BeEmpty();
        (await VersionAsync(scene.Items[0])).Should().Be(version);
    }

    // ---- UC-WM-D06 priority (a Select value) ----------------------------------------------------------------

    [Fact]
    public async Task Priority_RetriedWithTheSameKey_HasOneEffect_TheSameOptionChangesNothing_AndAnOptionOfAnotherFieldIsRefused()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var command = new UpdateBoardItemFieldValueCommand(scene.Items[0], scene.PriorityFieldId, scene.HighOptionId.ToString(), read);
        var key = $"priority-{Guid.NewGuid():N}";

        await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendIdempotentAsync(owner, command, key);
        await OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemFieldValueCommand(scene.Items[0], scene.PriorityFieldId, scene.HighOptionId.ToString(), afterFirst));
        var otherFieldsOption = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemFieldValueCommand(scene.Items[0], scene.PriorityFieldId, scene.DoneOptionId.ToString(), afterFirst));

        afterFirst.Should().Be(read + 1);
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay and the same option again wrote nothing");
        await otherFieldsOption.Should().ThrowAsync<Exception>("an option of the Status field is not a Priority option");
        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().Contain(scene.HighOptionId.ToString());
    }

    [Fact]
    public async Task Priority_WithAStaleVersion_IsRefusedAndNoValueIsStored()
    {
        var scene = await SeedAsync();
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[0], "Moved on", read));
        var current = await VersionAsync(scene.Items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner,
            new UpdateBoardItemFieldValueCommand(scene.Items[0], scene.PriorityFieldId, scene.HighOptionId.ToString(), read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().BeNull();
        (await VersionAsync(scene.Items[0])).Should().Be(current);
    }

    [Fact]
    public async Task Priority_ByAPersonOfAnotherWorkspace_NeverReachesTheItem()
    {
        var scene = await SeedAsync();
        var version = await VersionAsync(scene.Items[0]);

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider,
                new UpdateBoardItemFieldValueCommand(scene.Items[0], scene.PriorityFieldId, scene.HighOptionId.ToString(), version));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });

        (await StoredValueAsync(scene.Items[0], scene.PriorityFieldId)).Should().BeNull();
    }

    // ---- UC-WM-B11 delete, UC-WM-B12 restore, UC-WM-B13 Trash ---------------------------------------------

    [Fact]
    public async Task Delete_RetriedWithTheSameKey_HasOneEffect_AndAStaleVersionIsRefused()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.Items[0]);
        var key = $"delete-{Guid.NewGuid():N}";

        var secondRead = await VersionAsync(scene.Items[1]);
        await OrgTreeTestHost.SendAsync(owner, Rename(scene.Items[1], "Moved on", secondRead));
        var stale = () => OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[1], secondRead));
        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await IsDeletedAsync(scene.Items[1])).Should().BeFalse("a refused delete changes nothing");

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, new DeleteBoardItemCommand(scene.Items[0], read), key);
        var deletedAt = await DeletedAtAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, new DeleteBoardItemCommand(scene.Items[0], read), key);

        replay.Should().BeEquivalentTo(first, "the replay returns the first committed result");
        (await DeletedAtAsync(scene.Items[0])).Should().Be(deletedAt, "the replay deleted nothing a second time");
    }

    [Fact]
    public async Task Delete_ByAMemberWhoCannotReadThePrivateBoard_AnswersLikeAMissingItem()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(scene.Items[0]);
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, new DeleteBoardItemCommand(scene.Items[0], version));
        var missing = () => OrgTreeTestHost.SendAsync(provider, new DeleteBoardItemCommand(Guid.CreateVersion7(), version));

        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());
        (await IsDeletedAsync(scene.Items[0])).Should().BeFalse();
    }

    [Fact]
    public async Task Restore_RetriedWithTheSameKey_HasOneEffect_AndAnItemPastThirtyDaysIsRefused()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[0], await VersionAsync(scene.Items[0])));
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[1], await VersionAsync(scene.Items[1])));
        var key = $"restore-{Guid.NewGuid():N}";

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, new RestoreBoardItemCommand(scene.Items[0]), key);
        var versionAfterRestore = await VersionAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, new RestoreBoardItemCommand(scene.Items[0]), key);

        replay.Should().BeEquivalentTo(first);
        (await IsDeletedAsync(scene.Items[0])).Should().BeFalse();
        (await VersionAsync(scene.Items[0])).Should().Be(versionAfterRestore, "the replay restored nothing a second time");

        // 31 days later the second item is out of the window: refused with the reason, and it stays deleted.
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(31);
        using var later = Provider(scene.Stack);
        var tooLate = () => OrgTreeTestHost.SendAsync(later, new RestoreBoardItemCommand(scene.Items[1]));
        (await tooLate.Should().ThrowAsync<Conflict>()).Which.Message.Should().Contain("30 days");
        (await IsDeletedAsync(scene.Items[1])).Should().BeTrue();
    }

    [Fact]
    public async Task Restore_ByAMemberWhoCannotReadThePrivateBoard_AnswersLikeAMissingItem()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[0], await VersionAsync(scene.Items[0])));
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        using var provider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, new RestoreBoardItemCommand(scene.Items[0]));
        var missing = () => OrgTreeTestHost.SendAsync(provider, new RestoreBoardItemCommand(Guid.CreateVersion7()));

        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());
        (await IsDeletedAsync(scene.Items[0])).Should().BeTrue();
    }

    [Fact]
    public async Task Trash_ListsTheDeletedRootsNewestFirst_LeavesOutPastThirtyDays_AndHidesTheBoardFromOthers()
    {
        var scene = await SeedAsync(BoardVisibility.Private, itemCount: 3);
        using (var owner = Provider(scene.Stack))
        {
            await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[0], await VersionAsync(scene.Items[0])));
            _host.Now = OrgTreeTestHost.BaseTime.AddDays(10);
        }
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[1], await VersionAsync(scene.Items[1])));

        _host.Now = OrgTreeTestHost.BaseTime.AddDays(31);
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[2], await VersionAsync(scene.Items[2])));
        var trashed = (await OrgTreeTestHost.SendAsync(Provider(scene.Stack), new ListBoardItemTrashQuery(scene.BoardId)) as BoardItemTrashPageDto)!.Items;

        trashed.Select(e => e.Name).Should().Equal(["Task 2", "Task 1"], "newest first, and Task 0 is past the thirty-day window");
        trashed.Should().OnlyContain(e => e.RestorableUntil > _host.Now);

        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        using var memberProvider = _host.CreateProvider(scene.Stack.AccountId, member);
        var hidden = () => OrgTreeTestHost.SendAsync(memberProvider, new ListBoardItemTrashQuery(scene.BoardId));
        var missing = () => OrgTreeTestHost.SendAsync(memberProvider, new ListBoardItemTrashQuery(Guid.CreateVersion7()));
        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider, new ListBoardItemTrashQuery(scene.BoardId));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });
    }

    // ---- UC-WM-B10 archived list and unarchive -------------------------------------------------------------

    [Fact]
    public async Task Unarchive_RetriedWithTheSameKey_HasOneEffect_AStaleVersionIsRefused_AndTheListLeavesTheItemOut()
    {
        var scene = await SeedAsync(itemCount: 2);
        using var owner = Provider(scene.Stack);
        foreach (var id in scene.Items)
            await OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(id, await VersionAsync(id)));
        ((await OrgTreeTestHost.SendAsync(owner, new ListArchivedBoardItemsQuery(scene.BoardId)) as ArchivedBoardItemPageDto)!.Items).Should().HaveCount(2);

        var archivedVersion = await VersionAsync(scene.Items[0]);
        var key = $"unarchive-{Guid.NewGuid():N}";
        var stale = () => OrgTreeTestHost.SendAsync(owner, new UnarchiveBoardItemCommand(scene.Items[1], archivedVersion - 1));
        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await ItemAsync(scene.Items[1])).IsArchived.Should().BeTrue("a refused unarchive changes nothing");

        var first = await OrgTreeTestHost.SendIdempotentAsync(owner, new UnarchiveBoardItemCommand(scene.Items[0], archivedVersion), key);
        var afterFirst = await VersionAsync(scene.Items[0]);
        var replay = await OrgTreeTestHost.SendIdempotentAsync(owner, new UnarchiveBoardItemCommand(scene.Items[0], archivedVersion), key);

        replay.Should().BeEquivalentTo(first);
        (await VersionAsync(scene.Items[0])).Should().Be(afterFirst, "the replay unarchived nothing a second time");
        ((await OrgTreeTestHost.SendAsync(owner, new ListArchivedBoardItemsQuery(scene.BoardId)) as ArchivedBoardItemPageDto)!.Items)
            .Select(e => e.Id).Should().Equal(scene.Items[1]);
    }

    [Fact]
    public async Task ArchivedList_OfAPrivateBoard_IsNotFoundToAMemberWhoCannotReadIt_AndDeniedOutsideTheWorkspace()
    {
        var scene = await SeedAsync(BoardVisibility.Private);
        var member = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Member");
        using var memberProvider = _host.CreateProvider(scene.Stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(memberProvider, new ListArchivedBoardItemsQuery(scene.BoardId));
        var missing = () => OrgTreeTestHost.SendAsync(memberProvider, new ListArchivedBoardItemsQuery(Guid.CreateVersion7()));
        (await hidden.Should().ThrowAsync<AppNotFound>()).Which.GetType()
            .Should().Be((await missing.Should().ThrowAsync<AppNotFound>()).Which.GetType());

        await ForEachOutsiderAsync(scene.Stack, async provider =>
        {
            var act = () => OrgTreeTestHost.SendAsync(provider, new ListArchivedBoardItemsQuery(scene.BoardId));
            await act.Should().ThrowAsync<AppNotFound>("a person with no role in the Workspace gets the answer a missing item gets, never Forbidden");
        });
    }

    // ---- UC-WM-B10 and UC-WM-B13 pages, UC-WM-B12 sub-item restore -------------------------------------

    [Fact]
    public async Task TrashAndArchive_ArePagedByCursor_CountTheSubItemsAndNameWhoDidIt_AndTheArchiveSearchesNameAndKey()
    {
        var scene = await SeedAsync(itemCount: 3);
        var child = await AddSubItemAsync(scene, scene.Items[0], "Child");
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(scene.Items[0], await VersionAsync(scene.Items[0])));
        _host.Now = OrgTreeTestHost.BaseTime.AddMinutes(1);
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(scene.Items[1], await VersionAsync(scene.Items[1])));
        _host.Now = OrgTreeTestHost.BaseTime.AddMinutes(2);
        using (var owner = Provider(scene.Stack))
            await OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(scene.Items[2], await VersionAsync(scene.Items[2])));

        using var reader = Provider(scene.Stack);
        var first = (ArchivedBoardItemPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListArchivedBoardItemsQuery(scene.BoardId, Limit: 2)))!;
        first.Items.Select(e => e.Name).Should().Equal(["Task 2", "Task 1"], "newest first");
        first.Total.Should().Be(3);
        first.NextCursor.Should().NotBeNull();
        first.Items.Should().OnlyContain(e => e.ArchivedByName != null, "the person who archived is named");
        var second = (ArchivedBoardItemPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListArchivedBoardItemsQuery(scene.BoardId, Limit: 2, Cursor: first.NextCursor)))!;
        second.Items.Should().ContainSingle().Which.Name.Should().Be("Task 0");
        second.Items[0].SubItemCount.Should().Be(1, "the sub-item comes back with its parent");
        second.NextCursor.Should().BeNull("the page holds the rest");
        var found = (ArchivedBoardItemPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListArchivedBoardItemsQuery(scene.BoardId, Search: "task 1")))!;
        found.Items.Should().ContainSingle().Which.Name.Should().Be("Task 1");
        found.Total.Should().Be(1, "the count follows the search");
        var bad = () => OrgTreeTestHost.SendAsync(reader, new ListArchivedBoardItemsQuery(scene.BoardId, Cursor: "not-a-cursor"));
        await bad.Should().ThrowAsync<Exception>("a cursor nobody issued is refused");

        // The same two reads for the trash: the parent goes with its sub-item, so the sub-item is not a row of its own.
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(1);
        using (var owner = Provider(scene.Stack))
        {
            foreach (var id in scene.Items)
            {
                await OrgTreeTestHost.SendAsync(owner, new UnarchiveBoardItemCommand(id, await VersionAsync(id)));
                _host.Now = _host.Now.AddMinutes(1);
                await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(id, await VersionAsync(id)));
                _host.Now = _host.Now.AddMinutes(1);
            }
        }

        var firstTrash = (BoardItemTrashPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListBoardItemTrashQuery(scene.BoardId, Limit: 2)))!;
        firstTrash.Items.Select(e => e.Name).Should().Equal(["Task 2", "Task 1"]);
        firstTrash.Total.Should().Be(3);
        firstTrash.Items.Should().OnlyContain(e => e.DeletedByName != null);
        var lastTrash = (BoardItemTrashPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListBoardItemTrashQuery(scene.BoardId, Limit: 2, Cursor: firstTrash.NextCursor)))!;
        lastTrash.Items.Should().ContainSingle().Which.SubItemCount.Should().Be(1);
        lastTrash.NextCursor.Should().BeNull();
        var foundTrash = (BoardItemTrashPageDto)(await OrgTreeTestHost.SendAsync(reader, new ListBoardItemTrashQuery(scene.BoardId, Search: "TASK 1")))!;
        foundTrash.Items.Should().ContainSingle().Which.Name.Should().Be("Task 1");
        foundTrash.Total.Should().Be(1, "the count follows the search");
        _ = child;
    }

    [Fact]
    public async Task RestoreOfASubItem_WhileItsParentIsStillDeleted_IsRefusedAndChangesNothing_ThenComesBackWithTheParent()
    {
        var scene = await SeedAsync();
        var child = await AddSubItemAsync(scene, scene.Items[0], "Child");
        using var owner = Provider(scene.Stack);
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardItemCommand(scene.Items[0], await VersionAsync(scene.Items[0])));
        (await IsDeletedAsync(child)).Should().BeTrue("the sub-item went with its parent");

        var act = () => OrgTreeTestHost.SendAsync(owner, new RestoreBoardItemCommand(child));
        (await act.Should().ThrowAsync<Conflict>()).Which.ErrorCode.Should().Be("work-management.item.restore-parent-deleted");
        (await IsDeletedAsync(child)).Should().BeTrue("a refused restore changes nothing");

        await OrgTreeTestHost.SendAsync(owner, new RestoreBoardItemCommand(scene.Items[0]));
        (await IsDeletedAsync(child)).Should().BeFalse("the sub-item comes back with its parent");
    }

    private async Task<Guid> AddSubItemAsync(Scene scene, Guid parentId, string name)
    {
        var parent = await ItemAsync(parentId);
        var child = BoardItem.CreateSubItem(parent, name, FractionalIndex.Create("a0"), scene.Stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardItems.Add(child);
            return Task.CompletedTask;
        });
        return child.Id;
    }

    // ---- scene and helpers ---------------------------------------------------------------------------------

    private sealed record Scene(
        OrgStack Stack, Guid BoardId, List<Guid> Items, Guid PriorityFieldId, Guid HighOptionId, Guid DoneOptionId, Guid OtherGroupId, Guid StatusFieldId);

    private async Task<Scene> SeedAsync(BoardVisibility visibility = BoardVisibility.Workspace, int itemCount = 1)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Writes", visibility);
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var other = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Doing", Color.Create("#808080"), FractionalIndex.Create("a1"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var items = Enumerable.Range(0, itemCount)
            .Select(i => BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, $"Task {i}", FractionalIndex.Create($"a{i}"), stack.OwnerId, OrgTreeTestHost.BaseTime))
            .ToList();
        var priority = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Priority", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a1"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var high = priority.AddOption("High", Color.Create("#FF0000"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var status = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a2"), stack.OwnerId, OrgTreeTestHost.BaseTime, isSystem: true);
        status.AddDefaultStatusOptions(stack.OwnerId, OrgTreeTestHost.BaseTime);
        var done = status.Options.Single(o => o.Category == WorkStatusCategory.Done);
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardGroups.AddRange(group, other);
            c.BoardItems.AddRange(items);
            c.BoardFields.AddRange(priority, status);
            return Task.CompletedTask;
        });
        return new Scene(stack, boardId, items.Select(i => i.Id).ToList(), priority.Id, high.Id, done.Id, other.Id, status.Id);
    }

    private static UpdateBoardItemCommand Rename(Guid itemId, string title, long version) =>
        new(itemId, title, null, null, version);

    private ServiceProvider Provider(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddScoped<IWorkOrderingScopeLock>(sp => new PostgresWorkOrderingScopeLock(sp.GetRequiredService<ApplicationDbContext>()));
            services.AddScoped<WorkPlacementResolver>();
            services.AddScoped<Notrelix.Application.Features.WorkManagement.BoardItems.Services.MoveBoardItemUseCase>();
        });

    /// <summary>A person with no role in the Workspace, and the owner of another Workspace, each in their own scope.</summary>
    private async Task ForEachOutsiderAsync(OrgStack stack, Func<ServiceProvider, Task> act)
    {
        var outsider = await _host.AddOutsiderAsync(stack);
        var otherWorkspace = await _host.SeedWorkspaceAsync();
        foreach (var (user, account) in new[] { (outsider, stack.AccountId), (otherWorkspace.OwnerId, otherWorkspace.AccountId) })
        {
            using var provider = _host.CreateProvider(account, user);
            await act(provider);
        }
    }

    private Task<BoardItem> ItemAsync(Guid id) => _host.ReadAsync(c => c.BoardItems.AsNoTracking().SingleAsync(i => i.Id == id));

    private async Task<long> VersionAsync(Guid id) => (await ItemAsync(id)).Version;

    private Task<DateTimeOffset?> DeletedAtAsync(Guid id) =>
        _host.ReadAsync(async c => (await c.BoardItems.IgnoreQueryFilters().AsNoTracking().SingleAsync(i => i.Id == id)).DeletedAt);

    private async Task<bool> IsDeletedAsync(Guid id) => await DeletedAtAsync(id) is not null;

    private Task<List<Guid>> AssigneesAsync(Guid itemId) =>
        _host.ReadAsync(c => c.BoardItemMembers.AsNoTracking().Where(m => m.ItemId == itemId).Select(m => m.UserId).ToListAsync());

    private async Task<string?> StoredValueAsync(Guid itemId, Guid fieldId)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT value::text FROM work.board_item_values WHERE item_id = '{itemId}' AND field_id = '{fieldId}'", connection);
        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<List<T>> QueryListAsync<T>(ServiceProvider provider, object query) =>
        (List<T>)(await OrgTreeTestHost.SendAsync(provider, query))!;
}

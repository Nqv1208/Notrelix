using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Preferences.Commands.UpdateNotificationPreferences;
using Notrelix.Application.Features.Collaboration.Preferences.DTOs;
using Notrelix.Application.Features.Collaboration.Preferences.Queries.GetNotificationPreferences;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// Notification preference matrix use cases (accepted 2026-10-23): the caller reads and changes only their own
/// matrix, defaults fill every unchanged cell, a stale expected version is refused, and a request whose values
/// already hold saves nothing.
/// </summary>
public sealed class NotificationPreferencesHandlerTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    private readonly List<NotificationPreferenceSet> _rows = [];
    private readonly FakeCurrentRequestContext _request;
    private readonly Mock<ICollaborationDbContext> _db = new();

    public NotificationPreferencesHandlerTests()
    {
        _db.Setup(c => c.NotificationPreferenceSets).Returns(TestDbSet.Create(_rows).Object);
        _request = new FakeCurrentRequestContext { UserId = UserId };
        _request.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
    }

    private GetNotificationPreferencesQueryHandler Reader() => new(_db.Object, _request);

    private UpdateNotificationPreferencesCommandHandler Writer() =>
        new(_db.Object, _request, FakeDateTimeProvider.WithFixedTime(Now));

    private static NotificationPreferenceChange C(string type, string channel, bool enabled) => new(type, channel, enabled);

    private static NotificationPreferenceRowChange R(string type, bool? inApp, bool? email) => new(type, inApp, email);

    private static NotificationPreferenceSet SetOf(Guid user, params NotificationPreferenceChange[] changes) =>
        NotificationPreferenceSet.Create(AccountId, WorkspaceId, user, changes, Now)!;

    private static UpdateNotificationPreferencesCommand Update(long version, params NotificationPreferenceRowChange[] rows) =>
        new(WorkspaceId, version, rows);

    [Fact]
    public async Task Get_WithoutASavedChange_ReturnsTheSevenDefaultRowsAtVersionZero()
    {
        var result = await Reader().Handle(new GetNotificationPreferencesQuery(WorkspaceId), CancellationToken.None);

        var dto = result.Data!;
        dto.Version.Should().Be(0);
        dto.Rows.Select(r => r.Type).Should().Equal(NotificationTypes.All);
        dto.Rows.Should().OnlyContain(r => r.InApp);
        dto.Rows.Where(r => r.Email).Select(r => r.Type).Should().Equal("assigned", "mention", "approval", "invitation");
    }

    [Fact]
    public async Task Get_ReturnsTheCallersOwnChoices_NeverAnotherUsersOrAnotherWorkspaces()
    {
        _rows.Add(SetOf(UserId, C("mention", "in_app", false)));
        _rows.Add(SetOf(Guid.NewGuid(), C("assigned", "in_app", false)));
        _rows.Add(NotificationPreferenceSet.Create(AccountId, Guid.NewGuid(), UserId, [C("approval", "in_app", false)], Now)!);

        var dto = (await Reader().Handle(new GetNotificationPreferencesQuery(WorkspaceId), CancellationToken.None)).Data!;

        dto.Version.Should().Be(1);
        dto.Rows.Single(r => r.Type == "mention").InApp.Should().BeFalse();
        dto.Rows.Single(r => r.Type == "assigned").InApp.Should().BeTrue("another person's choice is not the caller's");
        dto.Rows.Single(r => r.Type == "approval").InApp.Should().BeTrue("another Workspace's choice is not this one's");
    }

    [Fact]
    public async Task Update_FirstChange_CreatesTheCallersSetAtVersionOne()
    {
        var result = await Writer().Handle(Update(0, R("mention", false, null), R("digest", null, true)), CancellationToken.None);

        _rows.Should().ContainSingle();
        _rows[0].UserId.Should().Be(UserId);
        _rows[0].AccountId.Should().Be(AccountId);
        _rows[0].WorkspaceId.Should().Be(WorkspaceId);
        var dto = result.Data!;
        dto.Version.Should().Be(1);
        dto.Rows.Single(r => r.Type == "mention").InApp.Should().BeFalse();
        dto.Rows.Single(r => r.Type == "digest").Email.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValuesThatAlreadyHold_SaveNothingAndKeepVersionZero()
    {
        var result = await Writer().Handle(Update(0, R("mention", true, true)), CancellationToken.None);

        _rows.Should().BeEmpty();
        result.Data!.Version.Should().Be(0);
    }

    [Fact]
    public async Task Update_ExistingSet_AppliesPartialChangeAndBumpsTheVersion()
    {
        _rows.Add(SetOf(UserId, C("mention", "in_app", false)));

        var dto = (await Writer().Handle(Update(1, R("overdue", null, true)), CancellationToken.None)).Data!;

        dto.Version.Should().Be(2);
        dto.Rows.Single(r => r.Type == "overdue").Email.Should().BeTrue();
        dto.Rows.Single(r => r.Type == "mention").InApp.Should().BeFalse("an earlier choice survives a partial update");
    }

    [Theory]
    [InlineData(0L, 1L)]
    [InlineData(5L, 1L)]
    public async Task Update_AStaleExpectedVersion_IsRefusedAndChangesNothing(long expected, long current)
    {
        _rows.Add(SetOf(UserId, C("mention", "in_app", false)));

        var act = () => Writer().Handle(Update(expected, R("overdue", null, true)), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<PreconditionFailedException>();
        thrown.Which.ExpectedVersion.Should().Be(expected);
        thrown.Which.CurrentVersion.Should().Be(current);
        _rows[0].Version.Should().Be(current);
    }

    [Fact]
    public async Task Update_AStaleVersionOnAMissingSet_IsRefused()
    {
        var act = () => Writer().Handle(Update(3, R("overdue", null, true)), CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_AnotherUsersSetIsNeverTouched()
    {
        var other = SetOf(Guid.NewGuid(), C("mention", "in_app", false));
        _rows.Add(other);

        await Writer().Handle(Update(0, R("overdue", null, true)), CancellationToken.None);

        _rows.Should().HaveCount(2);
        other.Version.Should().Be(1);
        other.IsEnabled("overdue", "email").Should().BeFalse();
    }

    [Fact]
    public void Validator_RequiresRowsAndANonNegativeVersion()
    {
        var validator = new UpdateNotificationPreferencesCommandValidator();

        validator.Validate(new UpdateNotificationPreferencesCommand(WorkspaceId, 0, Array.Empty<NotificationPreferenceRowChange>())).IsValid.Should().BeFalse();
        validator.Validate(new UpdateNotificationPreferencesCommand(WorkspaceId, -1, [R("mention", true, null)])).IsValid.Should().BeFalse();
        validator.Validate(new UpdateNotificationPreferencesCommand(WorkspaceId, 0, [R("mention", true, null)])).IsValid.Should().BeTrue();
    }
}

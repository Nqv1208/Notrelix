using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Retention.Commands.SetRetentionPolicy;
using Notrelix.Application.Features.Chat.Retention.Queries.GetRetentionPolicy;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Application.Tests.Features.Collaboration;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppPreconditionFailedException = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Application.Tests.Features.Chat.Retention;

/// <summary>UC-CHT-H05 (configure) and UC-CHT-H04 (the retention change is audited): the Owner sets the rule, everyone reads it.</summary>
public class RetentionPolicyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _ws = Guid.NewGuid();
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly List<RetentionPolicy> _policies = [];
    private readonly List<(string Action, AuditChange? Change)> _facts = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ICurrentRequestContext> _ctx = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IAuditService> _audit = new();

    public RetentionPolicyHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _ctx.Setup(c => c.RequireAccountId()).Returns(_account);
        _ctx.Setup(c => c.UserId).Returns(_owner);
        _db.Setup(d => d.ChatRetentionPolicies).Returns(() => TestDbSet.Create(_policies).Object);
        _members.Setup(m => m.FindAsync(_ws, _owner, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_ws, _member, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _audit.Setup(a => a.RecordAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ResourceRef>(), It.IsAny<AuditMetadata>(),
                It.IsAny<AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()))
            .Callback((Guid _, Guid _, string action, ResourceRef _, AuditMetadata _, AuditSeverity _, string _, string _, CancellationToken _, AuditChange? change) =>
                _facts.Add((action, change)))
            .Returns(Task.CompletedTask);
    }

    private SetRetentionPolicyCommandHandler Set() => new(_db.Object, _ctx.Object, _members.Object, _clock.Object, new ChatAuditFacts(_audit.Object, _ctx.Object));
    private GetRetentionPolicyQueryHandler Get() => new(_db.Object, _ctx.Object, _members.Object);

    [Fact]
    public async Task AWorkspaceWithoutARuleReadsAsNoRetentionAtVersionOne_AndOnlyTheOwnerCanChangeIt()
    {
        var owner = (await Get().Handle(new GetRetentionPolicyQuery(_ws), default)).Data!;
        _ctx.Setup(c => c.UserId).Returns(_member);
        var member = (await Get().Handle(new GetRetentionPolicyQuery(_ws), default)).Data!;

        owner.RetentionDays.Should().BeNull();
        owner.Mode.Should().Be("tombstone");
        owner.Version.Should().Be(1);
        owner.CanChange.Should().BeTrue();
        member.CanChange.Should().BeFalse("a member reads the rule but does not set it");
    }

    [Fact]
    public async Task TheOwnerSetsTheFirstRuleAtVersionOne_OneAuditFactWithOldAndNew()
    {
        var result = await Set().Handle(new SetRetentionPolicyCommand(_ws, 90, "purge", 1), default);

        result.Data!.RetentionDays.Should().Be(90);
        result.Data.Mode.Should().Be("purge");
        result.Data.Version.Should().Be(2);
        _policies.Should().ContainSingle();
        _facts.Should().ContainSingle().Which.Action.Should().Be(ChatAuditFacts.RetentionChanged);
        _facts[0].Change!.Before.Should().NotBeNull();
        _facts[0].Change!.After.Should().NotBeNull();
    }

    [Fact]
    public async Task AFirstRuleWithAStaleVersionIs412()
    {
        var act = () => Set().Handle(new SetRetentionPolicyCommand(_ws, 90, "purge", 7), default);

        await act.Should().ThrowAsync<AppPreconditionFailedException>();
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task RepeatingTheStoredRuleChangesNothingAndWritesNoFact()
    {
        var policy = RetentionPolicy.CreateDefault(_account, _ws, Now);
        policy.Change(30, RetentionMode.Tombstone, _owner, Now);
        _policies.Add(policy);

        var result = await Set().Handle(new SetRetentionPolicyCommand(_ws, 30, "tombstone", policy.Version), default);

        result.Data!.Version.Should().Be(policy.Version);
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task TurningRetentionOffIsAudited()
    {
        var policy = RetentionPolicy.CreateDefault(_account, _ws, Now);
        policy.Change(30, RetentionMode.Purge, _owner, Now);
        _policies.Add(policy);

        var result = await Set().Handle(new SetRetentionPolicyCommand(_ws, null, "purge", policy.Version), default);

        result.Data!.RetentionDays.Should().BeNull();
        _facts.Should().ContainSingle();
    }

    [Fact]
    public async Task ANonMemberIsNotFound()
    {
        _ctx.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => Get().Handle(new GetRetentionPolicyQuery(_ws), default);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public void TheCommandNeedsTheOwnerAction_AndTheQueryOnlyViewing()
    {
        new SetRetentionPolicyCommand(_ws, 30, "purge", 1).Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ManageWorkspace);
        new GetRetentionPolicyQuery(_ws).Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ViewWorkspace);
    }

    [Theory]
    [InlineData(30, "tombstone", 1, true)]
    [InlineData(null, "purge", 1, true)]
    [InlineData(6, "purge", 1, false)]
    [InlineData(4000, "purge", 1, false)]
    [InlineData(30, "shred", 1, false)]
    [InlineData(30, "purge", 0, false)]
    public void TheValidatorChecksWindowModeAndVersion(int? days, string mode, long version, bool valid)
    {
        new SetRetentionPolicyCommandValidator().Validate(new SetRetentionPolicyCommand(_ws, days, mode, version)).IsValid.Should().Be(valid);
    }
}

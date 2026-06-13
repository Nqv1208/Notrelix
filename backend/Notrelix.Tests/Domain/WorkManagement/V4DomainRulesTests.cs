using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Billing.Events;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class V4DomainRulesTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void WorkspaceMismatch_ShouldThrowException_WhenFormQuestionWorkspaceIdMismatchesForm()
    {
        var otherWorkspaceId = Guid.NewGuid();
        var form = Form.Create(_workspaceId, _boardId, "Contact Form", "contact-form", _actorId, _now);
        
        var question = FormQuestion.Create(
            otherWorkspaceId, 
            form.Id, 
            boardFieldId: null, 
            questionKey: "FullName", 
            label: "Your Full Name", 
            questionType: "text", 
            isRequired: true, 
            position: FractionalIndex.Initial(), 
            configJson: null);

        Action act = () => form.AddQuestion(question, _actorId, _now);

        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void ItemDependency_Create_ShouldThrow_WhenSelfReferencing()
    {
        var itemId = Guid.NewGuid();

        Action act = () => ItemDependency.Create(
            _workspaceId,
            _boardId,
            itemId,
            itemId, // self reference
            DependencyType.FinishToStart,
            0,
            _actorId,
            _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("An item cannot depend on itself.");
    }

    [Fact]
    public void DependencyRules_EnsureNoCycle_ShouldThrow_WhenCycleDetected()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        var itemC = Guid.NewGuid();

        // Graph: A -> B -> C -> A (introducing cycle)
        var dependencies = new Dictionary<Guid, List<Guid>>
        {
            { itemA, new List<Guid> { itemB } },
            { itemB, new List<Guid> { itemC } },
            { itemC, new List<Guid>() } // We want to add C -> A
        };

        Func<Guid, IEnumerable<Guid>> getDependencies = id => 
            dependencies.TryGetValue(id, out var list) ? list : Enumerable.Empty<Guid>();

        Action act = () => DependencyRules.EnsureNoCycle(itemC, itemA, getDependencies);

        act.Should().Throw<BusinessRuleException>().WithMessage("Adding this dependency would create a cycle.");
    }

    [Fact]
    public void BoardItem_SetTimeline_ShouldThrow_WhenDueDateIsBeforeStartDate()
    {
        var group = BoardGroup.Create(_workspaceId, _boardId, "Group", Color.Create("#0079BF"), FractionalIndex.Initial(), _actorId, _now);
        var item = BoardItem.Create(_workspaceId, _boardId, group.Id, "Item", FractionalIndex.Initial(), _actorId, _now);

        Action act = () => item.SetTimeline(_now, _now.AddDays(-1), _actorId, _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("Due date must be after start date.");
    }

    [Fact]
    public void TimeTrackingEntry_Stop_ShouldThrow_WhenTimerNotRunning()
    {
        var itemId = Guid.NewGuid();
        var entry = TimeTrackingEntry.Start(_workspaceId, _boardId, itemId, _actorId, _now);
        
        entry.Stop(_now.AddMinutes(5), _actorId);

        // Stopping it again should fail
        Action act = () => entry.Stop(_now.AddMinutes(10), _actorId);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot stop a timer that is not running.");
    }

    [Fact]
    public void TimeTrackingEntry_Stop_ShouldThrow_WhenEndTimeBeforeStartTime()
    {
        var itemId = Guid.NewGuid();
        var entry = TimeTrackingEntry.Start(_workspaceId, _boardId, itemId, _actorId, _now);

        Action act = () => entry.Stop(_now.AddMinutes(-5), _actorId);

        act.Should().Throw<BusinessRuleException>().WithMessage("End time must be after start time.");
    }

    [Fact]
    public void PermissionRule_IsActive_ShouldReturnFalse_WhenStartsAtIsFutureOrExpiresAtIsPast()
    {
        var ruleFuture = PermissionRule.Create(
            _workspaceId,
            "Workspace",
            null,
            null,
            "User",
            _actorId,
            null,
            "read",
            PermissionEffect.Allow,
            _actorId,
            _now,
            startsAt: _now.AddDays(1)); // future start

        var ruleExpired = PermissionRule.Create(
            _workspaceId,
            "Workspace",
            null,
            null,
            "User",
            _actorId,
            null,
            "read",
            PermissionEffect.Allow,
            _actorId,
            _now,
            expiresAt: _now.AddDays(-1)); // past expiry

        ruleFuture.IsActive(_now).Should().BeFalse();
        ruleExpired.IsActive(_now).Should().BeFalse();
    }

    [Fact]
    public void ShareLink_IsExpired_ShouldReturnTrue_WhenExpiryDatePassed()
    {
        var link = ShareLink.Create(
            _workspaceId,
            ResourceType.Board,
            _boardId,
            ShareLinkTokenHash.Create("tokenhash"),
            ShareLinkAccessMode.Public,
            _actorId,
            _now,
            expiresAt: _now.AddMinutes(-1));

        link.IsExpired(_now).Should().BeTrue();
    }

    [Fact]
    public void WorkspaceFeatureUsage_Consume_ShouldThrowAndRaiseEvent_WhenLimitExceeded()
    {
        var featureCode = FeatureCode.Create("Boards");
        var usage = WorkspaceFeatureUsage.Create(
            _workspaceId,
            featureCode,
            currentUsage: 8,
            hardLimit: 10,
            softLimit: null,
            overageAllowed: false);

        // Consume 3 items when current is 8 (8+3=11 > 10) should fail
        Action act = () => usage.Consume(3, _actorId, _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*feature usage limit exceeded*");
        
        usage.DomainEvents.Should().ContainSingle(e => e is QuotaExceededDomainEvent);
        var evt = (QuotaExceededDomainEvent)usage.DomainEvents.Single(e => e is QuotaExceededDomainEvent);
        evt.FeatureCode.Should().Be(featureCode.Code);
        evt.Limit.Should().Be(10);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Release_ShouldThrow_WhenGoingBelowZero()
    {
        var usage = WorkspaceFeatureUsage.Create(
            _workspaceId,
            FeatureCode.Create("Boards"),
            currentUsage: 2,
            hardLimit: 10,
            softLimit: null);

        Action act = () => usage.Release(3, _actorId, _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("Usage cannot be released below zero.");
    }
}

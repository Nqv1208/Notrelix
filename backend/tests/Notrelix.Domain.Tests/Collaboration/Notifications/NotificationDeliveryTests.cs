using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Domain.Tests.Collaboration;

public class NotificationDeliveryTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotificationChannel.Email, DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Pending);
        delivery.Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public void MarkSent_ShouldUpdateStatus()
    {
        var delivery = CreateDelivery();
        var providerId = "msg_123";

        delivery.MarkSent(providerId, DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Sent);
        delivery.ProviderMessageId.Should().Be(providerId);
        delivery.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkSent_WhenNotPending_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkSent(null, DateTimeOffset.UtcNow);

        var act = () => delivery.MarkSent("dup", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }

    [Fact]
    public void MarkFailed_ShouldUpdateStatus()
    {
        var delivery = CreateDelivery();

        delivery.MarkFailed("Connection timeout", DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Failed);
        delivery.ErrorMessage.Should().Be("Connection timeout");
    }

    [Fact]
    public void MarkFailed_WhenNotPending_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed("Error", DateTimeOffset.UtcNow);

        var act = () => delivery.MarkFailed("Another error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }

    [Fact]
    public void Skip_ShouldUpdateStatus()
    {
        var delivery = CreateDelivery();

        delivery.Skip(DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Skipped);
    }

    [Fact]
    public void Skip_WhenNotPending_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.Skip(DateTimeOffset.UtcNow);

        var act = () => delivery.Skip(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }

    [Fact]
    public void Cancel_ShouldUpdateStatus()
    {
        var delivery = CreateDelivery();

        delivery.Cancel(DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenNotPending_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.Cancel(DateTimeOffset.UtcNow);

        var act = () => delivery.Cancel(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }

    private static NotificationDelivery CreateDelivery()
    {
        return NotificationDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotificationChannel.InApp, DateTimeOffset.UtcNow);
    }
}

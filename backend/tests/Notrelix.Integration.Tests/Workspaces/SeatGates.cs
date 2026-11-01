using Notrelix.Application.Features.Billing.Public.Seats;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>The billing seat decision for hand-composed handlers whose test is about something else.</summary>
internal static class SeatGates
{
    public static IBillingSeatGate AlwaysAdmit { get; } =
        Mock.Of<IBillingSeatGate>(g =>
            g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true));
}

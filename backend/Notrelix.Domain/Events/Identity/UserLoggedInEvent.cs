using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Identity;

public class UserLoggedInEvent : BaseEvent
{
    public Guid UserId { get; }
    public string? DeviceInfo { get; }
    public string? IpAddress { get; }

    public UserLoggedInEvent(Guid userId, string? deviceInfo = null, string? ipAddress = null)
    {
        UserId = userId;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }
}

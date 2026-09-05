namespace Notrelix.Architecture.Tests.ScopingRules;

/// <summary>
/// TAC-DC-004 / DC-REF-004 — Features/Notifications is classified as a
/// technical notification-delivery support capability, NOT a bounded context.
///
/// Its exact Application inventory is two email-link-builder ports consumed by
/// the Infrastructure email dispatcher (Identity email verification and
/// Workspaces workspace invitations). Notification business semantics (who is
/// notified about which product fact) belong to the notifying context and its
/// outward events — never to this folder. The folder must not grow into a
/// shadow notification context.
/// </summary>
public class NotificationsClassificationTests
{
    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
    }

    [Fact]
    public void Notifications_IsNotACanonicalBusinessContext()
    {
        CrossContextBoundaryScanner.BusinessContexts.Should().NotContain("Notifications",
            "notification delivery plumbing is a support capability, not a bounded context");
    }

    [Fact]
    public void NotificationsPorts_StayInTheirExactFrozenInventory()
    {
        var appPath = GetApplicationPath();
        var notificationsRoot = Path.Combine(appPath, "Features", "Notifications");

        Directory.Exists(notificationsRoot).Should().BeTrue(
            "the classified notifications support module is expected to exist");

        var actual = Directory
            .EnumerateFiles(notificationsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName!)
            .ToHashSet(StringComparer.Ordinal);

        // Frozen at the TAC classification; new delivery-port families must go
        // through review and update this exact set in the same change.
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "IEmailVerificationLinkBuilder.cs",
            "IWorkspaceInvitationLinkBuilder.cs",
        };

        actual.Should().BeEquivalentTo(expected,
            "Features/Notifications is a classified support capability with an exact frozen " +
            "inventory — growth here would silently create a shadow notification context");
    }
}

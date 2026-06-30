using System.Text.RegularExpressions;

namespace Notrelix.Architecture.Tests;

public class LegacyCollabNotificationWriteBanTests
{
    private static string GetSolutionPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return current;
    }

    private static readonly string[] LegacyNotificationTypes =
    [
        "Notrelix.Domain.Collaboration.Notifications.Notification",
        "Notrelix.Domain.Collaboration.Notifications.NotificationPreference",
        "Notrelix.Domain.Collaboration.Notifications.NotificationDelivery",
    ];

    private static readonly string[] LegacyDbSetNames =
    [
        "Notifications",
        "NotificationPreferences",
        "NotificationDeliveries",
    ];

    [Fact]
    public void ApplicationHandlers_ShouldNotUseLegacyNotificationEntities()
    {
        var solutionPath = GetSolutionPath();
        var featuresPath = Path.Combine(solutionPath, "src", "Notrelix.Application", "Features");

        if (!Directory.Exists(featuresPath))
            return;

        var handlerFiles = Directory.GetFiles(featuresPath, "*Handler.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var cleaned = RemoveComments(content);

            foreach (var typeName in LegacyNotificationTypes)
            {
                var shortName = typeName.Split('.').Last();
                cleaned.Should().NotContain(
                    $"using {typeName}",
                    $"Handler '{Path.GetFileName(file)}' must not reference legacy notification type '{shortName}'.");
            }
        }
    }

    [Fact]
    public void ApplicationFeatures_ShouldNotImportLegacyNotificationNamespaces()
    {
        var solutionPath = GetSolutionPath();
        var featuresPath = Path.Combine(solutionPath, "src", "Notrelix.Application", "Features");

        if (!Directory.Exists(featuresPath))
            return;

        var csFiles = Directory.GetFiles(featuresPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);

            content.Should().NotContain(
                "using Notrelix.Domain.Collaboration.Notifications",
                $"File '{Path.GetFileName(file)}' must not import legacy Collaboration.Notifications namespace. Use Notrelix.Domain.Notifications instead.");
        }
    }

    [Fact]
    public void NewNotificationCode_ShouldUseCanonicalNotificationsNamespace()
    {
        var solutionPath = GetSolutionPath();
        var notificationsPath = Path.Combine(solutionPath, "src", "Notrelix.Domain", "Notifications");

        if (!Directory.Exists(notificationsPath))
            return;

        var csFiles = Directory.GetFiles(notificationsPath, "*.cs", SearchOption.AllDirectories);

        csFiles.Should().NotBeEmpty("Canonical Notifications domain folder should exist.");

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            content.Should().Contain(
                "namespace Notrelix.Domain.Notifications",
                $"File '{Path.GetFileName(file)}' should be in the Notrelix.Domain.Notifications namespace.");
        }
    }

    private static string RemoveComments(string code)
    {
        code = Regex.Replace(code, @"//.*$", "", RegexOptions.Multiline);
        code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return code;
    }
}

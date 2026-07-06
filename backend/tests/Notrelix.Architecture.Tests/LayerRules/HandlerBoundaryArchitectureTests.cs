namespace Notrelix.Architecture.Tests;

/// <summary>
/// Enforces handler injection rules:
/// - No IApplicationDbContext in handlers
/// - No ApplicationDbContext in handlers
/// - No Guid.Empty accountId in factory calls
/// </summary>
public class HandlerBoundaryArchitectureTests
{
    private static readonly string FeaturesPath = Path.Combine(
        FindProjectRoot(), "src", "Notrelix.Application", "Features");

    [Fact]
    public void Handlers_ShouldNotInject_IApplicationDbContext()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            // Skip DTOs, validators, commands, queries — only check handlers
            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            content.Should().NotContain("IApplicationDbContext _",
                $"Handler {relativePath} must not inject IApplicationDbContext");
        }
    }

    [Fact]
    public void Handlers_ShouldNotInject_ApplicationDbContext()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            content.Should().NotContain("ApplicationDbContext _",
                $"Handler {relativePath} must not inject ApplicationDbContext");
        }
    }

    [Fact]
    public void InlineDomainEventHandlers_MustNotDoIO()
    {
        var featureFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in featureFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!content.Contains("INotificationHandler<DomainEventNotification<"))
                continue;

            // Inline handlers must not inject IO services
            var ioServicePatterns = new[]
            {
                "IApplicationDbContext", "IAutomationDbContext",
                "DbContext", "IJobQueue",
                "IEmailService", "IHttpClientFactory",
                "HttpClient", "ISender"
            };

            foreach (var pattern in ioServicePatterns)
            {
                if (content.Contains(pattern))
                {
                    violations.Add($"{relativePath}: injects {pattern}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Inline domain event handlers must not perform IO. Use post-commit actions or outbox instead.");
    }

    [Fact]
    public void Handlers_ShouldNotUse_AccountIdEmpty_ForFactoryCalls()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            // Check for Guid.Empty used as accountId in factory Create calls
            // Pattern: .Create(Guid.Empty, ... or Create(\n            Guid.Empty,
            if (content.Contains(".Create(Guid.Empty,") ||
                content.Contains("Create(\n            Guid.Empty,"))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            "Handlers must use _tenant.RequireAccountId(), not Guid.Empty, for factory calls");
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }
}
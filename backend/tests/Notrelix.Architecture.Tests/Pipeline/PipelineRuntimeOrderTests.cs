using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Notrelix.Application.Common.Caching;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Common.Entitlements;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.PostCommit;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Infrastructure.Identity.Services;

namespace Notrelix.Architecture.Tests.Pipeline;

public class PipelineRuntimeOrderTests
{
    private record TestRequest : IRequest<TestResponse>, IAnonymousRequest;
    private record TestResponse;

    [Fact]
    public void Pipeline_ShouldResolveBehaviorsInRegistrationOrder()
    {
        var expectedOrder = ParseBehaviorRegistrationOrder();
        expectedOrder.Should().HaveCount(19, "expected exactly 19 pipeline behaviors");

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(Mock.Of<IExecutionContextReader>());
        services.AddSingleton<ICurrentTenantContext, CurrentTenantContext>();
        services.AddSingleton(Mock.Of<IResourceScopeResolver>());
        services.AddSingleton(Mock.Of<IPostCommitActionQueue>());
        services.AddSingleton<IRedisCacheService>(Mock.Of<IRedisCacheService>());
        services.AddSingleton(new CacheKeyFactory(Options.Create(new CacheKeyOptions())));
        services.AddSingleton<IRlsSessionContext>(Mock.Of<IRlsSessionContext>());
        services.AddSingleton<IIdempotencyStore>(Mock.Of<IIdempotencyStore>());
        services.AddSingleton<IRealtimePublisher>(Mock.Of<IRealtimePublisher>());
        services.AddSingleton<IPermissionVersionProvider>(Mock.Of<IPermissionVersionProvider>());
        services.AddSingleton<IResourceVersionReader>(Mock.Of<IResourceVersionReader>());
        services.AddSingleton(Mock.Of<ICurrentUser>());
        services.AddSingleton(Mock.Of<IIdentityUserLookupService>());
        services.AddSingleton<ISubscriptionChecker>(Mock.Of<ISubscriptionChecker>());
        services.AddSingleton<IFeatureGateChecker>(Mock.Of<IFeatureGateChecker>());
        services.AddSingleton<ITenantBootstrapStore>(Mock.Of<ITenantBootstrapStore>());
        services.AddSingleton<IAuthorizationDecisionStore>(Mock.Of<IAuthorizationDecisionStore>());
        services.AddSingleton<IPermissionService>(Mock.Of<IPermissionService>());
        services.AddSingleton<IApplicationDbContext>(Mock.Of<IApplicationDbContext>());
        services.AddSingleton<IRequestDataSession>(Mock.Of<IRequestDataSession>());
        services.AddSingleton<IEnumerable<IValidator<TestRequest>>>(Array.Empty<IValidator<TestRequest>>());

        foreach (var typeName in expectedOrder)
        {
            var behaviorType = FindBehaviorType(typeName);
            var closedType = behaviorType.MakeGenericType(typeof(TestRequest), typeof(TestResponse));
            services.AddTransient(typeof(IPipelineBehavior<TestRequest, TestResponse>), closedType);
        }

        var provider = services.BuildServiceProvider();
        var resolvedOrder = provider
            .GetServices<IPipelineBehavior<TestRequest, TestResponse>>()
            .Select(b => b.GetType().Name.Split('`')[0])
            .ToList();

        resolvedOrder.Should().HaveCount(19, "container should resolve 19 behaviors");
        resolvedOrder.Should().Equal(expectedOrder,
            "DI container must resolve behaviors in registration order — " +
            "if this fails, someone changed the order in DependencyInjection.cs or a decorator/interceptor is interfering");
    }

    private static List<string> ParseBehaviorRegistrationOrder()
    {
        var diFile = Path.Combine(GetApplicationPath(), "DependencyInjection.cs");
        var content = RemoveComments(File.ReadAllText(diFile));

        return content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Contains("AddTransient(typeof(IPipelineBehavior<"))
            .Select(ExtractBehaviorName)
            .ToList();
    }

    private static string ExtractBehaviorName(string line)
    {
        var match = Regex.Match(line, @"typeof\(\w+<,>\),\s*typeof\((\w+)<,>\)");
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Could not extract behavior name from: {line}");
    }

    private static Type FindBehaviorType(string name)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType($"Notrelix.Application.Common.Behaviors.{name}`2"))
            .FirstOrDefault(t => t is not null);

        return type ?? throw new InvalidOperationException($"Behavior type not found: {name}");
    }

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

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}

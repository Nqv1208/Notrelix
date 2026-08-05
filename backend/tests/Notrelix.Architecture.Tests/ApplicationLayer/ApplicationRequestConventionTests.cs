using System.Reflection;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// Scans every concrete MediatR request in the Application assembly and validates
/// that each one classifies successfully with no contract violations.
/// </summary>
public sealed class ApplicationRequestConventionTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(RequestExecutionClassifier).Assembly;

    [Fact]
    public void All_concrete_requests_must_classify_without_violations()
    {
        var requestTypes = GetConcreteRequestTypes();

        requestTypes.Should().NotBeEmpty(
            "Application assembly must contain MediatR request types");

        var violations = new List<string>();

        foreach (var type in requestTypes)
        {
            var instance = CreateMinimalInstance(type);
            if (instance is null)
            {
                violations.Add($"{type.FullName}: cannot create instance for classification");
                continue;
            }

            var profile = RequestExecutionClassifier.Classify(instance);
            var errors = ValidateProfile(profile);
            violations.AddRange(errors.Select(e => $"{type.FullName}: {e}"));
        }

        violations.Should().BeEmpty(
            "every Application request must have a valid execution profile. Violations:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void All_concrete_requests_must_have_at_most_one_principal_kind()
    {
        var violations = new List<string>();

        foreach (var type in GetConcreteRequestTypes())
        {
            var instance = CreateMinimalInstance(type);
            if (instance is null) continue;

            var profile = RequestExecutionClassifier.Classify(instance);
            var principalCount =
                (profile.IsAnonymous ? 1 : 0) +
                (profile.IsSystemInternal ? 1 : 0);

            if (principalCount > 1)
                violations.Add(
                    $"{type.FullName}: request is both anonymous and system-internal");
        }

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Transactional_requests_must_not_be_public_cacheable()
    {
        var violations = new List<string>();

        foreach (var type in GetConcreteRequestTypes())
        {
            var instance = CreateMinimalInstance(type);
            if (instance is null) continue;

            var profile = RequestExecutionClassifier.Classify(instance);

            if (profile.IsTransactional && profile.IsPublicCacheable)
                violations.Add(
                    $"{type.FullName}: transactional request cannot use public cache");
        }

        violations.Should().BeEmpty();
    }

    private static List<Type> GetConcreteRequestTypes()
    {
        return ApplicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static object? CreateMinimalInstance(Type type)
    {
        try
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
                return Activator.CreateInstance(type);

            var ctor = constructors
                .OrderBy(c => c.GetParameters().Length)
                .First();

            var parameters = ctor.GetParameters()
                .Select(p => GetDefaultValue(p.ParameterType))
                .ToArray();

            return ctor.Invoke(parameters);
        }
        catch
        {
            return null;
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(string)) return "test";
        if (type == typeof(long)) return 1L;
        if (type == typeof(int)) return 1;
        if (type == typeof(bool)) return false;
        if (type.IsValueType) return Activator.CreateInstance(type);
        return null;
    }

    private static List<string> ValidateProfile(RequestExecutionProfile profile)
    {
        var errors = new List<string>();

        if (profile.IsAnonymous && profile.IsSystemInternal)
            errors.Add("cannot be both anonymous and system-internal");

        if (profile.IsGlobal && profile.IsTenantScoped)
            errors.Add("global request cannot also be tenant-scoped");

        if (profile.IsGlobal && profile.RequiresPermission)
            errors.Add("global request cannot require permission");

        if (profile.IsRlsRead && !profile.IsTenantScoped)
            errors.Add("RLS read must combine with tenant-scoping");

        if (profile.IsTokenScoped && profile.IsTenantScoped)
            errors.Add("token-scoped cannot also be tenant-scoped");

        if (profile.IsAnonymous && profile.IsTenantScoped)
            errors.Add("anonymous cannot be tenant-scoped");

        if (profile.IsPublicCacheable && profile.IsTenantScoped)
            errors.Add("public cache cannot be tenant-scoped");

        if (profile.IsPublicCacheable && profile.IsAuthorizedCacheable)
            errors.Add("cannot use both public and authorized cache");

        if (profile.IsAuthorizedCacheable && profile.IsRealtimeRequest)
            errors.Add("cannot be both authorized-cacheable and realtime");

        return errors;
    }
}

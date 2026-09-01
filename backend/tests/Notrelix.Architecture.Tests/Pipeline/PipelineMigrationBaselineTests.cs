using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;

namespace Notrelix.Architecture.Tests.Pipeline;

public sealed class PipelineMigrationBaselineTests : ArchitectureTestBase
{
    private const string UpdateBaselineVariable = "NOTRELIX_UPDATE_PIPELINE_BASELINES";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    [Fact]
    public void RequestExecutionBaseline_MapsExactlyToFinalRequestClassifications()
    {
        var actual = BuildRequestBaseline();
        var baselinePath = GetArchitectureBaselinePath("request-execution-baseline.json");

        if (string.Equals(Environment.GetEnvironmentVariable(UpdateBaselineVariable), "1", StringComparison.Ordinal))
        {
            AssertOrUpdateBaseline("request-execution-baseline.json", actual);
            return;
        }

        var baseline = JsonSerializer.Deserialize<RequestBaselineEntry[]>(File.ReadAllText(baselinePath), JsonOptions)!;
        actual.Select(entry => entry.RequestType).Should().Equal(
            baseline.Select(entry => entry.RequestType),
            "the final registry must cover the exact frozen production request inventory");

        foreach (var oldEntry in baseline)
        {
            var finalEntry = actual.Single(entry => entry.RequestType == oldEntry.RequestType);
            finalEntry.Kind.Should().Be(oldEntry.Kind);
            finalEntry.ScopeMarkers.Should().Equal(oldEntry.ScopeMarkers);
            finalEntry.RequiresPermission.Should().Be(oldEntry.RequiresPermission);
            finalEntry.RequiresVerifiedEmail.Should().Be(oldEntry.RequiresVerifiedEmail);
            finalEntry.RequiresSubscription.Should().Be(oldEntry.RequiresSubscription);
            finalEntry.RequiresFeature.Should().Be(oldEntry.RequiresFeature);
            finalEntry.ExpectedVersion.Should().Be(oldEntry.ExpectedVersion);
            finalEntry.Idempotent.Should().Be(oldEntry.Idempotent);
            finalEntry.Realtime.Should().BeFalse();
            finalEntry.AuthorizedCache.Should().BeFalse();
            finalEntry.PublicCache.Should().BeFalse();
            finalEntry.HandlerTypes.Should().Equal(oldEntry.HandlerTypes);
            finalEntry.PrincipalMarkers.Should().Equal(ExpectedPrincipalMarkers(oldEntry));
            finalEntry.DataMarkers.Should().Equal(ExpectedDataMarkers(oldEntry));
        }
    }

    [Fact]
    public void AsyncCacheBaseline_IsFullyMigrated()
    {
        var path = GetArchitectureBaselinePath("async-cache-baseline.json");
        var baseline = JsonSerializer.Deserialize<AsyncCacheBaselineEntry[]>(File.ReadAllText(path), JsonOptions)!;
        var applicationAssembly = typeof(ICommand<>).Assembly;
        var mapperRequests = applicationAssembly.GetTypes()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRealtimeChangeMapper<,>))
            .Select(type => type.GenericTypeArguments[0].FullName)
            .ToArray();

        var realtimeRequests = baseline.Where(entry => entry.Kind == "realtime-request").ToArray();
        realtimeRequests.Should().HaveCount(8);
        foreach (var entry in realtimeRequests)
            mapperRequests.Count(name => name == entry.RequestType).Should().Be(1);

        var source = string.Join('\n', Directory.GetFiles(GetApplicationPath(), "*.cs", SearchOption.AllDirectories)
            .Where(pathValue => !IsGeneratedPath(pathValue))
            .Select(File.ReadAllText));
        source.Should().NotContain("IRealtimeRequest");
        source.Should().NotContain("IPostCommitActionQueue");
        source.Should().NotContain("IAuthorizedCacheableRequest");
        source.Should().NotContain("IPublicCacheableQuery");
    }

    [Fact]
    public void AccessControlBaseline_ReferencesExecutableCharacterizationScenarios()
    {
        var backendPath = Path.GetDirectoryName(GetSrcPath())!;
        var baselinePath = Path.Combine(
            backendPath,
            "tests",
            "Notrelix.Integration.Tests",
            "Baselines",
            "access-control-scenarios.json");
        var testSources = Directory.GetFiles(Path.Combine(backendPath, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Select(File.ReadAllText)
            .ToArray();

        using var baseline = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var scenarios = baseline.RootElement.EnumerateArray().ToArray();

        scenarios.Should().NotBeEmpty("the old access-control path must be frozen before replacement");
        scenarios.Select(item => item.GetProperty("scenario").GetString())
            .Should().OnlyHaveUniqueItems("scenario names are durable characterization identities");

        var missingTests = scenarios
            .Select(item => item.GetProperty("characterizationTest").GetString())
            .Where(testName => string.IsNullOrWhiteSpace(testName)
                || !testSources.Any(source => source.Contains($" {testName}(", StringComparison.Ordinal)))
            .ToArray();

        missingTests.Should().BeEmpty(
            "every access baseline entry must point to an executable old-path characterization test");

        scenarios.Select(item => item.GetProperty("principalKind").GetString())
            .Should().Contain(["Anonymous", "Authenticated", "System"]);
        scenarios.Select(item => item.GetProperty("scopeKind").GetString())
            .Should().Contain(["Global", "Account", "Workspace", "Resource", "Token"]);
        scenarios.Select(item => item.GetProperty("expectedDecision").GetString())
            .Should().Contain(["Allowed", "Forbidden", "NotFound", "SecurityMisconfiguration"]);
    }

    private static IReadOnlyList<RequestBaselineEntry> BuildRequestBaseline()
    {
        var applicationAssembly = typeof(ICommand<>).Assembly;
        var handlers = applicationAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(IsRequestHandlerInterface)
                .Select(handlerInterface => new
                {
                    RequestType = handlerInterface.GenericTypeArguments[0],
                    HandlerType = type,
                }))
            .GroupBy(entry => entry.RequestType)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.HandlerType.FullName!).Order().ToArray());

        return applicationAssembly.GetTypes()
            .Where(IsProductionRequest)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type =>
            {
                handlers.TryGetValue(type, out var handlerTypes);

                return new RequestBaselineEntry(
                    RequestType: type.FullName!,
                    Kind: ImplementsOpenGeneric(type, typeof(IQuery<>)) ? "Query" : "Command",
                    PrincipalMarkers: MarkerNames(type,
                        typeof(IAnonymousRequest),
                        typeof(IAuthenticatedRequest),
                        typeof(ISystemInternalRequest)),
                    ScopeMarkers: MarkerNames(type,
                        typeof(IGlobalRequest),
                        typeof(IAccountRequest),
                        typeof(IWorkspaceRequest),
                        typeof(IResourceScopedRequest),
                        typeof(ITokenScopedRequest)),
                    DataMarkers: MarkerNames(type,
                        typeof(INoDataRequest),
                        typeof(IReadRequest),
                        typeof(IWriteRequest)),
                    RequiresPermission: typeof(IRequirePermission).IsAssignableFrom(type),
                    RequiresVerifiedEmail: typeof(IRequireVerifiedEmail).IsAssignableFrom(type),
                    RequiresSubscription: typeof(IRequireSubscription).IsAssignableFrom(type),
                    RequiresFeature: typeof(IRequireFeature).IsAssignableFrom(type),
                    ExpectedVersion: typeof(IExpectedVersionRequest).IsAssignableFrom(type),
                    Idempotent: typeof(IIdempotentRequest).IsAssignableFrom(type),
                    Realtime: false,
                    AuthorizedCache: false,
                    PublicCache: false,
                    HandlerTypes: handlerTypes ?? []);
            })
            .ToArray();
    }

    private static IReadOnlyList<AsyncCacheBaselineEntry> BuildAsyncCacheBaseline()
    {
        var requestEntries = BuildRequestBaseline();
        var applicationPath = GetApplicationPath();
        var srcPath = GetSrcPath();
        var sourceFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var entries = new List<AsyncCacheBaselineEntry>();
        foreach (var request in requestEntries.Where(entry => entry.Realtime || entry.AuthorizedCache || entry.PublicCache))
        {
            var requestName = request.RequestType.Split('.').Last();
            var sourceFile = FindRequestSourceFile(sourceFiles, requestName);
            var source = sourceFile is null ? "source-not-found" : ExtractContractSource(sourceFile);
            var relativeSource = sourceFile is null
                ? null
                : Path.GetRelativePath(applicationPath, sourceFile).Replace('\\', '/');

            if (request.Realtime)
            {
                entries.Add(new AsyncCacheBaselineEntry(
                    Kind: "realtime-request",
                    RequestType: request.RequestType,
                    HandlerTypes: request.HandlerTypes,
                    SourceFile: relativeSource,
                    ContractSource: source,
                    MigrationStatus: "legacy"));
            }

            if (request.AuthorizedCache)
            {
                entries.Add(new AsyncCacheBaselineEntry(
                    Kind: "authorized-cache-request",
                    RequestType: request.RequestType,
                    HandlerTypes: request.HandlerTypes,
                    SourceFile: relativeSource,
                    ContractSource: source,
                    MigrationStatus: "legacy"));
            }

            if (request.PublicCache)
            {
                entries.Add(new AsyncCacheBaselineEntry(
                    Kind: "public-cache-request",
                    RequestType: request.RequestType,
                    HandlerTypes: request.HandlerTypes,
                    SourceFile: relativeSource,
                    ContractSource: source,
                    MigrationStatus: "legacy"));
            }
        }

        AddSourceUses(entries, sourceFiles, srcPath, "IRealtimePublisher", "realtime-publisher-use");
        AddSourceUses(entries, sourceFiles, srcPath, "IPostCommitActionQueue", "post-commit-queue-use");
        AddSourceUses(entries, sourceFiles, srcPath, "PostCommitEnqueueBehavior", "post-commit-behavior-reference");
        AddSourceUses(entries, sourceFiles, srcPath, "PostCommitScopeBehavior", "post-commit-behavior-reference");
        AddSourceUses(entries, sourceFiles, srcPath, "SystemOperationAuditBehavior", "system-audit-reference");

        return entries
            .OrderBy(entry => entry.Kind, StringComparer.Ordinal)
            .ThenBy(entry => entry.RequestType ?? entry.SourceFile, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddSourceUses(
        ICollection<AsyncCacheBaselineEntry> entries,
        IEnumerable<string> sourceFiles,
        string srcPath,
        string symbol,
        string kind)
    {
        foreach (var sourceFile in sourceFiles.Where(path => File.ReadAllText(path).Contains(symbol, StringComparison.Ordinal)))
        {
            entries.Add(new AsyncCacheBaselineEntry(
                Kind: kind,
                RequestType: null,
                HandlerTypes: [],
                SourceFile: Path.GetRelativePath(srcPath, sourceFile).Replace('\\', '/'),
                ContractSource: symbol,
                MigrationStatus: "legacy"));
        }
    }

    private static bool IsProductionRequest(Type type) =>
        type is { IsAbstract: false, IsInterface: false }
        && type.Namespace?.StartsWith("Notrelix.Application", StringComparison.Ordinal) == true
        && type.GetInterfaces().Any(IsRequestInterface);

    private static bool IsRequestInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>);

    private static bool IsRequestHandlerInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);

    private static bool ImplementsOpenGeneric(Type type, Type genericDefinition) =>
        type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericDefinition);

    private static string[] MarkerNames(Type type, params Type[] markerTypes) => markerTypes
        .Where(markerType => markerType.IsAssignableFrom(type))
        .Select(markerType => markerType.Name)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static string? FindRequestSourceFile(IEnumerable<string> sourceFiles, string requestName) =>
        sourceFiles.FirstOrDefault(path =>
            path.Contains($"{Path.DirectorySeparatorChar}Notrelix.Application{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && File.ReadAllText(path).Contains(requestName, StringComparison.Ordinal));

    private static string ExtractContractSource(string sourceFile)
    {
        var relevantTokens = new[] { "RealtimeTopic", "Topic", "CacheScope", "CacheIdentity", "CacheTtl", "Ttl" };
        var lines = File.ReadLines(sourceFile)
            .Select(line => line.Trim())
            .Where(line => relevantTokens.Any(token => line.Contains(token, StringComparison.Ordinal)))
            .Where(line => line.Length > 0)
            .ToArray();

        return lines.Length == 0 ? "marker-only" : string.Join(" ", lines);
    }

    private static bool IsGeneratedPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static void AssertOrUpdateBaseline<T>(string fileName, T actual)
    {
        var baselinePath = GetArchitectureBaselinePath(fileName);
        var baselineDirectory = Path.GetDirectoryName(baselinePath)!;
        var actualJson = JsonSerializer.Serialize(actual, JsonOptions) + Environment.NewLine;

        if (string.Equals(Environment.GetEnvironmentVariable(UpdateBaselineVariable), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(baselineDirectory);
            File.WriteAllText(baselinePath, actualJson);
            return;
        }

        File.Exists(baselinePath).Should().BeTrue(
            $"{fileName} is a mandatory pre-refactor oracle; generate it once with {UpdateBaselineVariable}=1");

        File.ReadAllText(baselinePath).Should().Be(
            actualJson,
            $"{fileName} must cover the exact legacy production inventory and must not be regenerated to hide drift");
    }

    private static string GetArchitectureBaselinePath(string fileName) => Path.Combine(
        Path.GetDirectoryName(GetSrcPath())!,
        "tests",
        "Notrelix.Architecture.Tests",
        "Baselines",
        fileName);

    private static string[] ExpectedPrincipalMarkers(RequestBaselineEntry oldEntry) =>
        oldEntry.PrincipalMarkers.Length == 0
            ? [nameof(IAuthenticatedRequest)]
            : oldEntry.PrincipalMarkers;

    private static string[] ExpectedDataMarkers(RequestBaselineEntry oldEntry)
    {
        // Entries baselined against the final marker model (no legacy
        // ITransactionalRequest/IRlsReadRequest history) compare as-is.
        if (oldEntry.DataMarkers.Contains("IWriteRequest", StringComparer.Ordinal)
            || oldEntry.DataMarkers.Contains("IReadRequest", StringComparer.Ordinal)
            || oldEntry.DataMarkers.Contains("INoDataRequest", StringComparer.Ordinal))
        {
            return oldEntry.DataMarkers;
        }

        if (oldEntry.DataMarkers.Contains("ITransactionalRequest", StringComparer.Ordinal))
        {
            return [nameof(IWriteRequest)];
        }

        if (oldEntry.DataMarkers.Contains("IRlsReadRequest", StringComparer.Ordinal))
        {
            return [nameof(IReadRequest)];
        }

        if (oldEntry.Kind == "Query" || oldEntry.RequestType.EndsWith(".ForgotPasswordCommand", StringComparison.Ordinal))
        {
            return [nameof(IReadRequest)];
        }

        if (oldEntry.RequestType.EndsWith(".ToggleChecklistItemCommand", StringComparison.Ordinal))
        {
            return [nameof(IWriteRequest)];
        }

        return [nameof(INoDataRequest)];
    }

    private sealed record RequestBaselineEntry(
        string RequestType,
        string Kind,
        string[] PrincipalMarkers,
        string[] ScopeMarkers,
        string[] DataMarkers,
        bool RequiresPermission,
        bool RequiresVerifiedEmail,
        bool RequiresSubscription,
        bool RequiresFeature,
        bool ExpectedVersion,
        bool Idempotent,
        bool Realtime,
        bool AuthorizedCache,
        bool PublicCache,
        string[] HandlerTypes);

    private sealed record AsyncCacheBaselineEntry(
        string Kind,
        string? RequestType,
        string[] HandlerTypes,
        string? SourceFile,
        string ContractSource,
        string MigrationStatus);
}

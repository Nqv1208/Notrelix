namespace Notrelix.Architecture.Tests;

public class CommonFolderArchitectureTests
{
    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
            current = Path.GetDirectoryName(current);
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
    }

    private static string CommonPath => Path.Combine(GetApplicationPath(), "Common");

    [Fact]
    public void AbstractionsFolder_ShouldNotExist()
    {
        var absPath = Path.Combine(CommonPath, "Abstractions");
        Directory.Exists(absPath).Should().BeFalse("Abstractions/ folder should be removed; INotificationService has been deleted");
    }

    [Fact]
    public void PipelineFolder_ShouldNotExist()
    {
        var pipelinePath = Path.Combine(CommonPath, "Pipeline");
        Directory.Exists(pipelinePath).Should().BeFalse("Pipeline/ folder was deleted; TenantBootstrapBehavior moved to Behaviors/");
    }

    [Fact]
    public void TransactionsFolder_ShouldNotExist()
    {
        var path = Path.Combine(CommonPath, "Transactions");
        Directory.Exists(path).Should().BeFalse("Transactions/ folder was deleted (empty placeholder)");
    }

    [Fact]
    public void ExtensionsFolder_ShouldNotExist()
    {
        var path = Path.Combine(CommonPath, "Extensions");
        Directory.Exists(path).Should().BeFalse("Extensions/ folder was deleted (empty placeholder)");
    }

    [Fact]
    public void MappingFolder_ShouldNotExist()
    {
        var path = Path.Combine(CommonPath, "Mapping");
        Directory.Exists(path).Should().BeFalse("Mapping/ folder was deleted (empty placeholder)");
    }

    [Fact]
    public void ReadModelsFolder_ShouldNotExist()
    {
        var path = Path.Combine(CommonPath, "ReadModels");
        Directory.Exists(path).Should().BeFalse("ReadModels/ folder was deleted (empty placeholder)");
    }

    [Fact]
    public void ValidationFolder_ShouldNotExist()
    {
        var path = Path.Combine(CommonPath, "Validation");
        Directory.Exists(path).Should().BeFalse("Validation/ folder was deleted (empty placeholder)");
    }

    [Fact]
    public void CacheInvalidationKey_ShouldBeInCaching()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "CacheInvalidationKey.cs");
        var cachingPath = Path.Combine(CommonPath, "Caching", "CacheInvalidationKey.cs");
        File.Exists(cqrsPath).Should().BeFalse("CacheInvalidationKey moved from Requests/ to Caching/");
        File.Exists(cachingPath).Should().BeTrue("CacheInvalidationKey should exist in Caching/");
    }

    [Fact]
    public void RealtimeTopic_ShouldBeInPostCommit()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "RealtimeTopic.cs");
        var postCommitPath = Path.Combine(CommonPath, "PostCommit", "RealtimeTopic.cs");
        File.Exists(cqrsPath).Should().BeFalse("RealtimeTopic moved from Requests/ to PostCommit/");
        File.Exists(postCommitPath).Should().BeTrue("RealtimeTopic should exist in PostCommit/");
    }

    [Fact]
    public void FeatureCode_ShouldBeInEntitlements()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "FeatureCode.cs");
        var entitlementsPath = Path.Combine(CommonPath, "Entitlements", "FeatureCode.cs");
        File.Exists(cqrsPath).Should().BeFalse("FeatureCode moved from Requests/ to Entitlements/");
        File.Exists(entitlementsPath).Should().BeTrue("FeatureCode should exist in Entitlements/");
    }

    [Fact]
    public void IActivityRequest_ShouldBeInActivity()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "IActivityRequest.cs");
        var activityPath = Path.Combine(CommonPath, "Activity", "IActivityRequest.cs");
        File.Exists(cqrsPath).Should().BeFalse("IActivityRequest moved from Requests/ to Activity/");
        File.Exists(activityPath).Should().BeTrue("IActivityRequest should exist in Activity/");
    }

    [Fact]
    public void IAuditableRequest_ShouldBeInAuditing()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "IAuditableRequest.cs");
        var auditingPath = Path.Combine(CommonPath, "Auditing", "IAuditableRequest.cs");
        File.Exists(cqrsPath).Should().BeFalse("IAuditableRequest moved from Requests/ to Auditing/");
        File.Exists(auditingPath).Should().BeTrue("IAuditableRequest should exist in Auditing/");
    }

    [Fact]
    public void IMessageTriggeredRequest_ShouldBeInMessaging()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests", "IMessageTriggeredRequest.cs");
        var messagingPath = Path.Combine(CommonPath, "Messaging", "IMessageTriggeredRequest.cs");
        File.Exists(cqrsPath).Should().BeFalse("IMessageTriggeredRequest moved from Requests/ to Messaging/");
        File.Exists(messagingPath).Should().BeTrue("IMessageTriggeredRequest should exist in Messaging/");
    }

    [Fact]
    public void NoIRequireEntitlement_InRequests()
    {
        var path = Path.Combine(CommonPath, "Requests", "IRequireEntitlement.cs");
        File.Exists(path).Should().BeFalse("IRequireEntitlement was deleted; superseded by IRequireSubscription + IRequireFeature");
    }

    [Fact]
    public void NoIInvalidateCacheRequest_InRequests()
    {
        var path = Path.Combine(CommonPath, "Requests", "IInvalidateCacheRequest.cs");
        File.Exists(path).Should().BeFalse("IInvalidateCacheRequest was deleted; cache invalidation is ad-hoc via IPostCommitActionQueue");
    }

    [Fact]
    public void TenantBootstrapBehavior_ShouldBeInBehaviors()
    {
        var pipelinePath = Path.Combine(CommonPath, "Pipeline", "TenantBootstrapBehavior.cs");
        var behaviorsPath = Path.Combine(CommonPath, "Behaviors", "TenantBootstrapBehavior.cs");
        File.Exists(pipelinePath).Should().BeFalse("TenantBootstrapBehavior moved from Pipeline/ to Behaviors/");
        File.Exists(behaviorsPath).Should().BeTrue("TenantBootstrapBehavior should exist in Behaviors/");
    }

    [Fact]
    public void IPostCommitActionQueue_ShouldBeInPostCommit()
    {
        var contextPath = Path.Combine(CommonPath, "Context", "IPostCommitActionQueue.cs");
        var postCommitPath = Path.Combine(CommonPath, "PostCommit", "IPostCommitActionQueue.cs");
        File.Exists(contextPath).Should().BeFalse("IPostCommitActionQueue moved from Context/ to PostCommit/");
        File.Exists(postCommitPath).Should().BeTrue("IPostCommitActionQueue should exist in PostCommit/");
    }

    [Fact]
    public void N8nSignatureService_ShouldBeInIntegrations()
    {
        var securityPath = Path.Combine(CommonPath, "Security", "N8nSignatureService.cs");
        var n8nPath = Path.Combine(CommonPath, "Integrations", "N8n", "N8nSignatureService.cs");
        File.Exists(securityPath).Should().BeFalse("N8nSignatureService moved from Security/ to Integrations/N8n/");
        File.Exists(n8nPath).Should().BeTrue("N8nSignatureService should exist in Integrations/N8n/");
    }

    [Fact]
    public void AuthResult_ShouldBeInSecurityAuth()
    {
        var modelsPath = Path.Combine(CommonPath, "Models", "AuthResult.cs");
        var authPath = Path.Combine(CommonPath, "Security", "Auth", "AuthResult.cs");
        File.Exists(modelsPath).Should().BeFalse("AuthResult moved from Models/ to Security/Auth/");
        File.Exists(authPath).Should().BeTrue("AuthResult should exist in Security/Auth/");
    }

    [Fact]
    public void FileUploadResult_ShouldBeInStorage()
    {
        var modelsPath = Path.Combine(CommonPath, "Models", "FileUploadResult.cs");
        var storagePath = Path.Combine(CommonPath, "Storage", "FileUploadResult.cs");
        File.Exists(modelsPath).Should().BeFalse("FileUploadResult moved from Models/ to Storage/");
        File.Exists(storagePath).Should().BeTrue("FileUploadResult should exist in Storage/");
    }

    [Fact]
    public void Requests_ShouldNotContainMovedOrDeletedFiles()
    {
        var cqrsPath = Path.Combine(CommonPath, "Requests");
        var files = Directory.GetFiles(cqrsPath, "*.cs")
            .Select(Path.GetFileName)
            .ToHashSet();

        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FeatureCode.cs",
            "CacheInvalidationKey.cs",
            "RealtimeTopic.cs",
            "IActivityRequest.cs",
            "IAuditableRequest.cs",
            "IMessageTriggeredRequest.cs",
            "IRequireEntitlement.cs",
            "IInvalidateCacheRequest.cs",
        };

        var violations = files.Intersect(forbidden).ToArray();
        violations.Should().BeEmpty(
            $"Requests/ should not contain moved/deleted files: {string.Join(", ", violations)}");
    }

    [Fact]
    public void PipelineBehaviorCount_ShouldBeSixteen()
    {
        var diFile = Path.Combine(GetApplicationPath(), "DependencyInjection.cs");
        var content = File.ReadAllText(diFile);
        var lines = content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Contains("AddTransient(typeof(IPipelineBehavior<"))
            .ToList();

        lines.Should().HaveCount(19, "expected exactly 19 pipeline behaviors");
    }

    [Fact]
    public void RequestContractGuardBehavior_Must_Use_RequestExecutionClassifier()
    {
        var file = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "RequestContractGuardBehavior.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("RequestExecutionClassifier.Classify",
            "RequestContractGuardBehavior must use RequestExecutionClassifier instead of self-classifying request markers.");
        content.Should().NotContain("request is IGlobalRequest",
            "RequestContractGuardBehavior must not self-check marker interfaces — delegate to RequestExecutionClassifier.");
    }

    [Fact]
    public void DbRequestScopeBehavior_Must_Use_RequestExecutionClassifier()
    {
        var file = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "DbRequestScopeBehavior.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("RequestExecutionClassifier.Classify",
            "DbRequestScopeBehavior must use RequestExecutionClassifier instead of self-classifying request markers.");
        content.Should().NotContain("request is ITransactionalRequest",
            "DbRequestScopeBehavior must not self-check marker interfaces — delegate to RequestExecutionClassifier.");
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }

    [Fact]
    public void RequestContractGuardBehavior_Must_Run_Before_PublicCacheBehavior()
    {
        var diFile = Path.Combine(GetApplicationPath(), "DependencyInjection.cs");
        var lines = File.ReadAllLines(diFile)
            .Select(l => l.Trim())
            .Where(l => l.Contains("AddTransient(typeof(IPipelineBehavior<"))
            .Select(l => l.Contains("RequestContractGuardBehavior") ? "Guard"
                : l.Contains("PublicCacheBehavior") ? "PublicCache"
                : l.Contains("DbRequestScopeBehavior") ? "DbScope"
                : l.Contains("AuthorizationBehavior") ? "Auth"
                : null)
            .OfType<string>()
            .ToList();

        var guardIndex = lines.IndexOf("Guard");
        var publicCacheIndex = lines.IndexOf("PublicCache");

        guardIndex.Should().BeLessThan(publicCacheIndex,
            "RequestContractGuardBehavior must be registered BEFORE PublicCacheBehavior in the pipeline order. " +
            "Otherwise, public cache could serve tenant-scoped data before the guard validates the contract.");
    }
}

namespace Notrelix.Application.Tests.Common.Caching;

public class CacheKeyBuilderTests
{
    [Fact]
    public void Build_WithAllScopeParts_ReturnsFormattedKey()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var key = CacheKeyBuilder.Build(
            CacheScope.WorkspaceUser,
            "GetBoardQuery",
            "abc123",
            accountId: accountId,
            workspaceId: workspaceId,
            userId: userId,
            permissionVersion: "perm_v2",
            environment: "production");

        key.Should().StartWith("notrelix:v1:production:workspaceuser:");
        key.Should().Contain(accountId.ToString());
        key.Should().Contain(workspaceId.ToString());
        key.Should().Contain(userId.ToString());
        key.Should().Contain("perm_v2");
        key.Should().Contain("GetBoardQuery");
        key.Should().Contain("abc123");
    }

    [Fact]
    public void Build_PublicScope_DoesNotIncludeIds()
    {
        var key = CacheKeyBuilder.Build(
            CacheScope.Public,
            "GetPublicData",
            "def456",
            environment: "production");

        key.Should().Be("notrelix:v1:production:public:::::GetPublicData:def456");
    }

    [Fact]
    public void Build_WorkspaceScope_DiffersByWorkspaceId()
    {
        var ws1 = Guid.NewGuid();
        var ws2 = Guid.NewGuid();

        var key1 = CacheKeyBuilder.Build(CacheScope.Workspace, "Q", "h", workspaceId: ws1);
        var key2 = CacheKeyBuilder.Build(CacheScope.Workspace, "Q", "h", workspaceId: ws2);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void Build_UserScope_DiffersByUserId()
    {
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();

        var key1 = CacheKeyBuilder.Build(CacheScope.User, "Q", "h", userId: u1);
        var key2 = CacheKeyBuilder.Build(CacheScope.User, "Q", "h", userId: u2);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void Build_PermissionSensitiveKey_DiffersByPermissionVersion()
    {
        var key1 = CacheKeyBuilder.Build(CacheScope.WorkspaceUserPermission, "Q", "h", permissionVersion: "v1");
        var key2 = CacheKeyBuilder.Build(CacheScope.WorkspaceUserPermission, "Q", "h", permissionVersion: "v2");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void BuildHash_ReturnsConsistentHash()
    {
        var hash1 = CacheKeyBuilder.BuildHash("test-value");
        var hash2 = CacheKeyBuilder.BuildHash("test-value");

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(16); // 16 hex chars (8 bytes)
    }

    [Fact]
    public void BuildHash_DifferentInputs_Differ()
    {
        var hash1 = CacheKeyBuilder.BuildHash("value-a");
        var hash2 = CacheKeyBuilder.BuildHash("value-b");

        hash1.Should().NotBe(hash2);
    }
}

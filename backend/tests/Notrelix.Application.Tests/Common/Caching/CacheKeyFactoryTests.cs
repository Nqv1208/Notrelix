using Microsoft.Extensions.Options;

namespace Notrelix.Application.Tests.Common.Caching;

public class CacheKeyFactoryTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static CacheKeyFactory CreateFactory(string? environment = null, string? prefix = null, int schemaVersion = 1)
    {
        var options = Options.Create(new CacheKeyOptions
        {
            Environment = environment ?? "test",
            Prefix = prefix ?? "notrelix",
            SchemaVersion = schemaVersion
        });
        return new CacheKeyFactory(options);
    }

    [Fact]
    public void Public_GeneratesCorrectFormat()
    {
        var factory = CreateFactory();
        var result = factory.Public("Notrelix.Application.GetPlanQuery", "abc123");

        result.Should().Be("notrelix:v1:test:public:_:_:_:_:Notrelix.Application.GetPlanQuery:abc123");
    }

    [Fact]
    public void Workspace_GeneratesCorrectFormat()
    {
        var factory = CreateFactory();
        var result = factory.Workspace(AccountId, WorkspaceId, "Notrelix.Application.GetBoardQuery", "def456");

        var expected = $"notrelix:v1:test:workspace:{AccountId}:{WorkspaceId}:_:_:Notrelix.Application.GetBoardQuery:def456";
        result.Should().Be(expected);
    }

    [Fact]
    public void User_GeneratesCorrectFormat()
    {
        var factory = CreateFactory();
        var result = factory.User(AccountId, WorkspaceId, UserId, "Notrelix.Application.GetMyQuery", "ghi789");

        var expected = $"notrelix:v1:test:user:{AccountId}:{WorkspaceId}:{UserId}:_:Notrelix.Application.GetMyQuery:ghi789";
        result.Should().Be(expected);
    }

    [Fact]
    public void Permissioned_GeneratesCorrectFormat()
    {
        var factory = CreateFactory();
        var result = factory.Permissioned(AccountId, WorkspaceId, UserId, "v2", "Notrelix.Application.GetSensitiveQuery", "jkl012");

        var expected = $"notrelix:v1:test:permissioned:{AccountId}:{WorkspaceId}:{UserId}:v2:Notrelix.Application.GetSensitiveQuery:jkl012";
        result.Should().Be(expected);
    }

    [Fact]
    public void Public_WithCustomOptions_UsesOptions()
    {
        var factory = CreateFactory(environment: "staging", prefix: "myapp", schemaVersion: 2);
        var result = factory.Public("MyQuery", "hash1");

        result.Should().Be("myapp:v2:staging:public:_:_:_:_:MyQuery:hash1");
    }

    [Fact]
    public void Account_GeneratesCorrectFormat()
    {
        var factory = CreateFactory();
        var result = factory.Account(AccountId, "Notrelix.Application.GetAccountQuery", "mno345");

        var expected = $"notrelix:v1:test:account:{AccountId}:_:_:_:Notrelix.Application.GetAccountQuery:mno345";
        result.Should().Be(expected);
    }

    [Fact]
    public void Account_ThrowsOnEmptyAccountId()
    {
        var factory = CreateFactory();
        var act = () => factory.Account(Guid.Empty, "Q", "h");
        act.Should().Throw<ArgumentException>().WithMessage("*AccountId*");
    }

    [Fact]
    public void Workspace_ThrowsOnEmptyAccountId()
    {
        var factory = CreateFactory();
        var act = () => factory.Workspace(Guid.Empty, WorkspaceId, "Q", "h");
        act.Should().Throw<ArgumentException>().WithMessage("*AccountId*");
    }

    [Fact]
    public void Workspace_ThrowsOnEmptyWorkspaceId()
    {
        var factory = CreateFactory();
        var act = () => factory.Workspace(AccountId, Guid.Empty, "Q", "h");
        act.Should().Throw<ArgumentException>().WithMessage("*WorkspaceId*");
    }

    [Fact]
    public void User_ThrowsOnEmptyUserId()
    {
        var factory = CreateFactory();
        var act = () => factory.User(AccountId, WorkspaceId, Guid.Empty, "Q", "h");
        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    [Fact]
    public void Permissioned_ThrowsOnEmptyPermissionVersion()
    {
        var factory = CreateFactory();
        var act = () => factory.Permissioned(AccountId, WorkspaceId, UserId, "", "Q", "h");
        act.Should().Throw<ArgumentException>().WithMessage("*PermissionVersion*");
    }

    [Fact]
    public void BuildHash_IsDeterministic()
    {
        var factory = CreateFactory();
        var hash1 = factory.BuildHash(new { BoardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") });
        var hash2 = factory.BuildHash(new { BoardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") });

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void BuildHash_DifferentInputs_DifferentHashes()
    {
        var factory = CreateFactory();
        var hash1 = factory.BuildHash(new { BoardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") });
        var hash2 = factory.BuildHash(new { BoardId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void BuildHash_Produces16CharHex()
    {
        var factory = CreateFactory();
        var hash = factory.BuildHash(new { X = 1 });

        hash.Should().HaveLength(16);
        hash.Should().MatchRegex("^[0-9a-f]{16}$");
    }
}

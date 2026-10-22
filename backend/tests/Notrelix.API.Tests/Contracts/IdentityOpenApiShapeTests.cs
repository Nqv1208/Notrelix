using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// Identity auth contract (D10) asserted on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json, regenerated and diffed by CI):
/// every Identity operation declares typed success and problem responses (no
/// <c>System.Void</c>), problem responses are <c>application/problem+json</c>,
/// the credential operations that can be throttled declare 429, and the
/// browser session body never carries a token (tokens travel only as httpOnly
/// cookies).
/// </summary>
public class IdentityOpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> IdentityOperations()
    {
        foreach (var path in Document.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
            {
                if (!method.Value.TryGetProperty("tags", out var tags))
                    continue;
                if (tags.EnumerateArray().Any(t => t.GetString()!.StartsWith("Identity.", StringComparison.Ordinal)))
                    yield return (path.Name, method.Name, method.Value);
            }
    }

    [Fact]
    public void IdentityOperations_AreDiscovered()
    {
        IdentityOperations().Count().Should().BeGreaterThan(30, "a zero-operation sweep proves nothing (NRX-016)");
    }

    [Fact]
    public void NoIdentityOperation_ExportsAnUntypedVoidResponse()
    {
        var offenders = IdentityOperations()
            .Where(o => o.Operation.GetRawText().Contains("System.Void", StringComparison.Ordinal))
            .Select(o => $"{o.Method.ToUpperInvariant()} {o.Path}")
            .ToArray();

        offenders.Should().BeEmpty("typed responses are required for Identity (D10)");
    }

    [Fact]
    public void EveryIdentityOperation_DeclaresAtLeastOneProblemResponse()
    {
        var offenders = IdentityOperations()
            // Public bootstrap-style reads with no failure mode of their own are listed explicitly.
            .Where(o => !o.Operation.GetProperty("responses").EnumerateObject().Any(r =>
                r.Name.StartsWith('4') &&
                r.Value.TryGetProperty("content", out var c) && c.TryGetProperty("application/problem+json", out _)))
            .Select(o => $"{o.Method.ToUpperInvariant()} {o.Path}")
            .ToArray();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void EveryDeclaredIdentityProblemResponse_IsProblemJson()
    {
        var offenders = IdentityOperations()
            .SelectMany(o => o.Operation.GetProperty("responses").EnumerateObject()
                .Where(r => r.Name.StartsWith('4') || r.Name.StartsWith('5'))
                .Where(r => !r.Value.TryGetProperty("content", out var c) || !c.TryGetProperty("application/problem+json", out _))
                .Select(r => $"{o.Method.ToUpperInvariant()} {o.Path} {r.Name}"))
            .ToArray();

        offenders.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Identity.Auth.Login", "AuthSessionResponse")]
    [InlineData("Identity.Auth.Register", "AuthSessionResponse")]
    [InlineData("Identity.Auth.RefreshToken", "AuthSessionResponse")]
    [InlineData("Identity.Mfa.VerifyChallenge", "AuthSessionResponse")]
    [InlineData("Identity.Auth.GetCurrentUser", "UserDto")]
    [InlineData("Identity.Auth.GetBootstrap", "BootstrapResult")]
    [InlineData("Identity.Auth.IssueCsrfToken", "CsrfTokenResponse")]
    [InlineData("Identity.Mfa.Status", "MfaStatusDto")]
    [InlineData("Identity.Sessions.List", "SessionInfoDto[]")]
    [InlineData("Identity.Profile.UpdateProfile", "UserDto")]
    public void BodyReturningOperation_DeclaresItsResponseType(string operationId, string expected)
    {
        var schema = Operation(operationId).GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Describe(schema).Should().Be(expected, operationId);
    }

    [Theory]
    [InlineData("Identity.Auth.ForgotPassword")]
    [InlineData("Identity.Auth.ResetPassword")]
    [InlineData("Identity.Auth.Logout")]
    [InlineData("Identity.Auth.ChangePassword")]
    [InlineData("Identity.Mfa.Disable")]
    [InlineData("Identity.Sessions.Revoke")]
    [InlineData("Identity.Sessions.RevokeOthers")]
    public void BodylessCommand_DeclaresAnEmpty200(string operationId)
    {
        var ok = Operation(operationId).GetProperty("responses").GetProperty("200");
        ok.TryGetProperty("content", out _).Should().BeFalse($"{operationId} returns no body");
    }

    [Theory]
    [InlineData("Identity.Auth.Login")]
    [InlineData("Identity.Auth.Register")]
    [InlineData("Identity.Auth.RefreshToken")]
    [InlineData("Identity.Auth.ForgotPassword")]
    [InlineData("Identity.Auth.ResetPassword")]
    [InlineData("Identity.Mfa.VerifyChallenge")]
    public void ThrottledAnonymousCredentialOperation_Declares429(string operationId)
    {
        Operation(operationId).GetProperty("responses").TryGetProperty("429", out _)
            .Should().BeTrue($"{operationId} is rate limited and locks out");
    }

    [Theory]
    [InlineData("Identity.Auth.Login")]
    [InlineData("Identity.Mfa.VerifyChallenge")]
    public void CredentialOperation_DeclaresUnauthorizedOrValidationProblems(string operationId)
    {
        var responses = Operation(operationId).GetProperty("responses");
        (responses.TryGetProperty("401", out _) || responses.TryGetProperty("400", out _)).Should().BeTrue(operationId);
    }

    [Fact]
    public void AuthSessionResponse_NeverCarriesATokenField()
    {
        var properties = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("Notrelix.API.Contracts.Identity.AuthSessionResponse")
            .GetProperty("properties").EnumerateObject().Select(p => p.Name).ToArray();

        properties.Should().NotContain(new[] { "accessToken", "refreshToken" });
        properties.Should().Contain(new[] { "mfaRequired", "mfaChallengeToken", "user", "expiresAt" });
    }

    private static string Describe(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            return reference.GetString()!.Split('/').Last().Split('.').Last();
        if (schema.TryGetProperty("type", out var type) && type.GetString() == "array")
            return Describe(schema.GetProperty("items")) + "[]";
        if (schema.TryGetProperty("format", out var format))
            return format.GetString()!;
        return schema.GetProperty("type").GetString()!;
    }

    private static JsonElement Operation(string operationId)
    {
        foreach (var (_, _, operation) in IdentityOperations())
            if (operation.TryGetProperty("operationId", out var id) && id.GetString() == operationId)
                return operation;
        throw new InvalidOperationException($"Operation {operationId} is not in the contract.");
    }

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found (NRX-016)");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}

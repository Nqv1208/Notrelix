namespace Notrelix.Architecture.Tests;

public class AuthEndpointContractArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void RegisterEndpoint_DoesNotBindCommandDirectly()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "RegisterEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().NotContain("RegisterCommand command",
            "RegisterEndpoint must bind RegisterRequest DTO, not RegisterCommand directly.");
        content.Should().Contain("RegisterRequest request",
            "RegisterEndpoint must bind RegisterRequest DTO.");
    }

    [Fact]
    public void RegisterEndpoint_UsesCookieService()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "RegisterEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("ICookieService",
            "RegisterEndpoint must set auth cookies after successful registration.");
        content.Should().Contain("SetTokenCookie",
            "RegisterEndpoint must call SetTokenCookie after successful registration.");
    }

    [Fact]
    public void RegisterEndpoint_HandlerHasCorrectParameters()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "RegisterEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("RegisterRequest request", "First parameter must be RegisterRequest DTO");
        content.Should().Contain("ISender sender", "Second parameter must be ISender");
        content.Should().Contain("ICookieService cookieService", "Third parameter must be ICookieService");
    }

    [Fact]
    public void LoginEndpoint_DoesNotBindCommandDirectly()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "LoginEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().NotContain("LoginCommand command",
            "LoginEndpoint must bind LoginRequest DTO, not LoginCommand directly.");
        content.Should().Contain("LoginRequest request",
            "LoginEndpoint must bind LoginRequest DTO.");
    }

    [Fact]
    public void LoginEndpoint_UsesCookieService()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "LoginEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("ICookieService",
            "LoginEndpoint must set auth cookies after successful login.");
        content.Should().Contain("SetTokenCookie",
            "LoginEndpoint must call SetTokenCookie after successful login.");
    }
}

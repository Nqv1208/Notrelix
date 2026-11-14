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
    public void RegisterEndpoint_OpensNoSession()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "RegisterEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().NotContain("ICookieService",
            "registration answers one neutral response for a new and an existing address and sets no auth cookie (UC-IDN-B03).");
        content.Should().NotContain("SetTokenCookie",
            "a session would tell a new address from an existing one.");
    }

    [Fact]
    public void RegisterEndpoint_HandlerHasCorrectParameters()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "Identity", "Auth", "Commands", "RegisterEndpoint.cs");
        var content = RemoveComments(File.ReadAllText(file));

        content.Should().Contain("RegisterRequest request", "First parameter must be RegisterRequest DTO");
        content.Should().Contain("ISender sender", "Second parameter must be ISender");
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

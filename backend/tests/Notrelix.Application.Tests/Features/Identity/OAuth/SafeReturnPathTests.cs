using Notrelix.Application.Features.Identity.OAuth;

namespace Notrelix.Application.Tests.Features.Identity.OAuth;

public class SafeReturnPathTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/home")]
    [InlineData("/workspaces/w1/boards/b1")]
    [InlineData("/boards/42?tab=list&sort=due#row-3")]
    [InlineData("/a//b")]
    public void IsSafe_AcceptsInternalRootRelativePaths(string path)
    {
        SafeReturnPath.IsSafe(path).Should().BeTrue();
        SafeReturnPath.OrNull(path).Should().Be(path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("//evil.com")]
    [InlineData("//evil.com/phish")]
    [InlineData("/\\evil.com")]
    [InlineData("/ok\\evil")]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>1</script>")]
    [InlineData("evil.com")]
    [InlineData("/%2F%2Fevil.com")]
    [InlineData("/%2f/evil.com")]
    [InlineData("/%2F/evil.com")]
    [InlineData("/%5Cevil.com")]
    [InlineData("/%5cevil.com")]
    [InlineData("/%252F%252Fevil.com")]
    [InlineData("/%25252F%25252Fevil.com")]
    [InlineData("/ok\r\nSet-Cookie: x=1")]
    [InlineData("/ok%0d%0aSet-Cookie:x=1")]
    [InlineData("/ok\tevil")]
    [InlineData("/ok\0evil")]
    [InlineData("/%E0%A4%A")]
    public void IsSafe_RefusesAnythingABrowserCouldResolveToAnotherOrigin(string? path)
    {
        SafeReturnPath.IsSafe(path).Should().BeFalse();
        SafeReturnPath.OrNull(path).Should().BeNull();
    }

    [Fact]
    public void IsSafe_CapsTheLength()
    {
        SafeReturnPath.IsSafe("/" + new string('a', SafeReturnPath.MaxLength - 1)).Should().BeTrue();
        SafeReturnPath.IsSafe("/" + new string('a', SafeReturnPath.MaxLength)).Should().BeFalse();
    }
}

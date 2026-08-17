
namespace Notrelix.Testing.Application.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "test@notrelix.com";
    public string Name { get; set; } = "Test User";
    public bool IsAuthenticated { get; set; } = true;
    public Guid? WorkspaceId { get; set; }
    public Guid? SessionId { get; set; }
}

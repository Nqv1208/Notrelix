namespace Notrelix.Application.Common.Interfaces;

/// <summary>
/// Cung cấp thông tin về user hiện tại từ HttpContext (JWT claims)
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
}

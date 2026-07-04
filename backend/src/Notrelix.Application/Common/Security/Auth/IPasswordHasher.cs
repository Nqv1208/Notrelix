namespace Notrelix.Application.Common.Security.Auth
{

    // Interface cho Password Hashing Service
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}

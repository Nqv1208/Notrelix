namespace Notrelix.Application.Common.Abstractions
{

    // Interface cho Password Hashing Service
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}

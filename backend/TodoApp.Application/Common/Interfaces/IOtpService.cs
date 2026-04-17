namespace TodoApp.Application.Common.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAsync(string purpose, string identifier);
    Task<bool> ValidateAsync(string purpose, string identifier, string code);
    Task<int> GetAttemptsAsync(string purpose, string identifier);
}

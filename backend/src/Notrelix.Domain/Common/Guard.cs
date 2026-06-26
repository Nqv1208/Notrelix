using System.Runtime.CompilerServices;

namespace Notrelix.Domain.Common;

public static class Guard
{
    public static void NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : class
    {
        if (value is null)
            throw new BusinessRuleException($"Parameter '{paramName}' cannot be null.");
    }

    public static void NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("Value cannot be null or whitespace.");
    }

    public static void NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
            throw new BusinessRuleException("GUID cannot be empty.");
    }

    public static void Positive(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            throw new BusinessRuleException("Value must be positive.");
    }

    public static void NotNegative(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
            throw new BusinessRuleException("Value must not be negative.");
    }

    public static void MaxLength(string? value, int maxLength, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is not null && value.Length > maxLength)
            throw new BusinessRuleException($"Value exceeds maximum length of {maxLength}.");
    }

    public static void InRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new BusinessRuleException($"Value must be between {min} and {max}.");
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new BusinessRuleException(message);
    }
}

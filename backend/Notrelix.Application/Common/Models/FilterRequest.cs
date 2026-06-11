namespace Notrelix.Application.Common.Models;

public record FilterRequest(
    string Field,
    string Operator,
    string Value
);

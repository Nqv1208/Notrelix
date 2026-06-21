using FluentValidation;
using MediatR;
using Notrelix.Application.Common.Behaviors;
using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record TestRequest(string Name);
    public sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var validators = new[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("");

        Func<Task> act = () => behavior.Handle(
            request, ct => throw new InvalidOperationException("should not be called"), default);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Keys.Any(k => k == "Name"));
    }

    [Fact]
    public async Task Handle_WhenRequestValid_PassesThrough()
    {
        var validators = new[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("Valid Name");

        var result = await behavior.Handle(
            request, ct => Task.FromResult("success"), default);

        result.Should().Be("success");
    }

    [Fact]
    public async Task Handle_WhenNoValidators_PassesThrough()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());

        var result = await behavior.Handle(
            new TestRequest(""), ct => Task.FromResult("ok"), default);

        result.Should().Be("ok");
    }
}

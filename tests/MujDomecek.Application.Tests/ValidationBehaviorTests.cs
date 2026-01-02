using FluentValidation;
using MujDomecek.Application.Behaviors;

namespace MujDomecek.Application.Tests;

public sealed class ValidationBehaviorTests
{
    private sealed record TestRequest(string? Name);

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
        var called = false;

        var response = await behavior.Handle(new TestRequest("ok"), () =>
        {
            called = true;
            return Task.FromResult("next");
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("next", response);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(new IValidator<TestRequest>[] { new TestValidator() });
        var called = false;

        var response = await behavior.Handle(new TestRequest("ok"), () =>
        {
            called = true;
            return Task.FromResult("next");
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("next", response);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(new IValidator<TestRequest>[] { new TestValidator() });
        var called = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestRequest(string.Empty), () =>
            {
                called = true;
                return Task.FromResult("next");
            }, CancellationToken.None));

        Assert.False(called);
        Assert.Contains(nameof(TestRequest.Name), exception.Errors.Select(e => e.PropertyName));
    }
}

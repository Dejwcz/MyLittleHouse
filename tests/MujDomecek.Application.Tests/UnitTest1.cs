using MujDomecek.Application.Common;

namespace MujDomecek.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_SetsIsSuccessAndNullError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SetsIsSuccessFalseAndError()
    {
        var result = Result.Failure("boom");

        Assert.False(result.IsSuccess);
        Assert.Equal("boom", result.Error);
    }
}

public sealed class ResultOfTTests
{
    [Fact]
    public void Success_SetsValueAndNullError()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SetsDefaultValueAndError()
    {
        var result = Result<int>.Failure("boom");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal("boom", result.Error);
    }
}

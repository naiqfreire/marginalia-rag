using Marginalia.Api.Common;
using Xunit;

namespace Marginalia.Api.Tests.Common;

public class ResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Ok_sets_success_and_null_error()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Fail_sets_error_with_message_and_code()
    {
        var result = Result.Fail("boom", "CODE_X");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("boom", result.Error.Message);
        Assert.Equal("CODE_X", result.Error.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Result_of_T_Ok_carries_value()
    {
        var result = Result<string>.Ok("payload");

        Assert.True(result.IsSuccess);
        Assert.Equal("payload", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Result_of_T_Fail_has_null_value_and_error()
    {
        var result = Result<string>.Fail("nope", "CODE_Y");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("nope", result.Error!.Message);
        Assert.Equal("CODE_Y", result.Error.Code);
    }
}

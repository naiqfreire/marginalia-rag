using Marginalia.Api.Features.IngestPage;
using Xunit;

namespace Marginalia.Api.Tests.Features.IngestPage;

public class IngestPageValidatorTests
{
    private readonly IngestPageValidator _validator = new();

    private static IngestPageRequest Valid() =>
        new("session-1", "https://example.com/a", "Title", "Some page text.");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Valid_request_passes()
    {
        var result = await _validator.ValidateAsync(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Empty_sessionId_fails(string? sessionId)
    {
        var request = Valid() with { SessionId = sessionId! };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.SessionId));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Empty_url_fails(string? url)
    {
        var request = Valid() with { Url = url! };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.Url));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Url_longer_than_2048_fails()
    {
        var request = Valid() with { Url = new string('a', 2049) };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.Url));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Url_exactly_2048_passes()
    {
        var request = Valid() with { Url = new string('a', 2048) };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Empty_title_fails(string? title)
    {
        var request = Valid() with { Title = title! };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.Title));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Title_longer_than_512_fails()
    {
        var request = Valid() with { Title = new string('a', 513) };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.Title));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Empty_text_fails(string? text)
    {
        var request = Valid() with { Text = text! };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestPageRequest.Text));
    }
}

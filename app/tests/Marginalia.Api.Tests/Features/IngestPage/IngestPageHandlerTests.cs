using Marginalia.Api.Common;
using Marginalia.Api.Features.IngestPage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marginalia.Api.Tests.Features.IngestPage;

public class IngestPageHandlerTests
{
    private static IngestPageHandler CreateHandler(int maxPageWords = 10_000) =>
        new(
            new IngestPageValidator(),
            Options.Create(new IngestPageLimits { MaxPageWords = maxPageWords }),
            NullLogger<IngestPageHandler>.Instance);

    private static IngestPageCommand ValidCommand(string? text = null) =>
        new("session-1", "https://example.com/a", "Title", text ?? "Hello world from a page.");

    private static string Words(int count) =>
        string.Join(" ", Enumerable.Repeat("word", count));

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Valid_command_returns_ok_with_accepted_true()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("session-1", result.Value.SessionId);
        Assert.True(result.Value.Accepted);
        Assert.Null(result.Error);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Invalid_command_returns_VALIDATION_FAILED()
    {
        var command = ValidCommand() with { Title = "" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Error!.Code);
        Assert.Null(result.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Text_at_exact_word_limit_returns_ok()
    {
        var handler = CreateHandler(maxPageWords: 100);

        var result = await handler.Handle(ValidCommand(Words(100)), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Text_one_word_over_limit_returns_TEXT_TOO_LONG()
    {
        var handler = CreateHandler(maxPageWords: 100);

        var result = await handler.Handle(ValidCommand(Words(101)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TEXT_TOO_LONG", result.Error!.Code);
        Assert.Contains("100", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Validation_runs_before_word_limit()
    {
        var handler = CreateHandler(maxPageWords: 5);
        var command = ValidCommand(Words(50)) with { Title = "" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Error!.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Whitespace_separators_count_as_single_boundaries()
    {
        var handler = CreateHandler(maxPageWords: 3);
        var text = "one\t two\n\nthree";

        var result = await handler.Handle(ValidCommand(text), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using Marginalia.Api.Common;
using Marginalia.Api.Features.IngestPage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Marginalia.Api.Tests.Features.IngestPage;

public class IngestPageConfigurationTests
{
    private static object Payload(string text) => new
    {
        sessionId = "session-1",
        url = "https://example.com/article",
        title = "Article title",
        text
    };

    private static string Words(int count) =>
        string.Join(" ", Enumerable.Repeat("word", count));

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Max_page_words_setting_enforces_custom_limit()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("MAX_PAGE_WORDS", "5"));
        var client = factory.CreateClient();

        var overLimit = await client.PostAsJsonAsync("/api/ingest", Payload(Words(6)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, overLimit.StatusCode);
        var error = await overLimit.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("TEXT_TOO_LONG", error!.Code);

        var atLimit = await client.PostAsJsonAsync("/api/ingest", Payload(Words(5)));
        Assert.Equal(HttpStatusCode.OK, atLimit.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Unhandled_exception_returns_500_INTERNAL_ERROR_without_details()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IValidator<IngestPageRequest>>();
                services.AddSingleton<IValidator<IngestPageRequest>>(new ThrowingValidator());
            }));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingest", Payload("hello"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INTERNAL_ERROR", body.Code);
        Assert.Equal("Internal server error", body.Error);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", raw, StringComparison.Ordinal);
        Assert.DoesNotContain(".cs", raw, StringComparison.Ordinal);
    }

    private sealed class ThrowingValidator : AbstractValidator<IngestPageRequest>
    {
        public override Task<ValidationResult> ValidateAsync(
            ValidationContext<IngestPageRequest> context,
            CancellationToken cancellation = default) =>
            throw new InvalidOperationException("validator failure for test");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Marginalia.Api.Common;
using Marginalia.Api.Features.IngestPage;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Marginalia.Api.Tests.Features.IngestPage;

public class IngestPageEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IngestPageEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static string Words(int count) =>
        string.Join(" ", Enumerable.Repeat("word", count));

    private static object ValidPayload(string? text = null) => new
    {
        sessionId = "session-1",
        url = "https://example.com/article",
        title = "Article title",
        text = text ?? "Readable page body."
    };

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Valid_payload_returns_200_with_accepted_body()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingest", ValidPayload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IngestPageResponse>();
        Assert.NotNull(body);
        Assert.Equal("session-1", body.SessionId);
        Assert.True(body.Accepted);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("sessionId", "")]
    [InlineData("url", "")]
    [InlineData("title", "")]
    [InlineData("text", "")]
    public async Task Empty_required_field_returns_400_with_error_contract(string field, string value)
    {
        var client = _factory.CreateClient();
        var payload = new Dictionary<string, string>
        {
            ["sessionId"] = "session-1",
            ["url"] = "https://example.com/article",
            ["title"] = "Article title",
            ["text"] = "Readable page body."
        };
        payload[field] = value;

        var response = await client.PostAsJsonAsync("/api/ingest", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_FAILED", body.Code);
        Assert.False(string.IsNullOrWhiteSpace(body.Error));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Url_over_2048_chars_returns_400()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            sessionId = "session-1",
            url = new string('a', 2049),
            title = "Article title",
            text = "Readable page body."
        };

        var response = await client.PostAsJsonAsync("/api/ingest", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_FAILED", body!.Code);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Title_over_512_chars_returns_400()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            sessionId = "session-1",
            url = "https://example.com/article",
            title = new string('a', 513),
            text = "Readable page body."
        };

        var response = await client.PostAsJsonAsync("/api/ingest", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_FAILED", body!.Code);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Text_over_default_word_limit_returns_422()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingest", ValidPayload(Words(10_001)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("TEXT_TOO_LONG", body.Code);
        Assert.False(string.IsNullOrWhiteSpace(body.Error));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Text_at_default_word_limit_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingest", ValidPayload(Words(10_000)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Invalid_fields_take_precedence_over_word_limit()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            sessionId = "session-1",
            url = "https://example.com/article",
            title = "",
            text = Words(10_001)
        };

        var response = await client.PostAsJsonAsync("/api/ingest", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_FAILED", body!.Code);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Missing_required_property_returns_400_with_error_contract()
    {
        var client = _factory.CreateClient();
        var json = """{"sessionId":"session-1","url":"https://example.com/a","title":"T"}""";

        var response = await client.PostAsync(
            "/api/ingest",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Code));
        Assert.False(string.IsNullOrWhiteSpace(body.Error));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Malformed_json_returns_400_with_error_contract()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/ingest",
            new StringContent("{not-json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Code));
        Assert.False(string.IsNullOrWhiteSpace(body.Error));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Error_bodies_never_leak_internals()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingest", ValidPayload(""));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", raw, StringComparison.Ordinal);
        Assert.DoesNotContain(".cs", raw, StringComparison.Ordinal);
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Marginalia.Api.Tests.Features.IngestPage;

public class IngestPageCorsTests
{
    private const string AllowedExtensionOrigin = "chrome-extension://abcdefghijklmnop";
    private const string DevOrigin = "http://localhost:5173";

    private static WebApplicationFactory<Program> Factory(string origins) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("CORS_ALLOWED_ORIGINS", origins));

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/ingest");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Allowed_origin_preflight_returns_matching_allow_origin_header()
    {
        var client = Factory($"{AllowedExtensionOrigin},{DevOrigin}").CreateClient();

        var response = await client.SendAsync(Preflight(AllowedExtensionOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            AllowedExtensionOrigin,
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Disallowed_origin_gets_no_allow_origin_header()
    {
        var client = Factory(AllowedExtensionOrigin).CreateClient();

        var response = await client.SendAsync(Preflight("http://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Wildcard_extension_origin_is_filtered_out()
    {
        var client = Factory($"chrome-extension://*,{DevOrigin}").CreateClient();

        var wildcardOrigin = await client.SendAsync(Preflight(AllowedExtensionOrigin));
        var devOrigin = await client.SendAsync(Preflight(DevOrigin));

        Assert.False(wildcardOrigin.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(
            DevOrigin,
            devOrigin.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}

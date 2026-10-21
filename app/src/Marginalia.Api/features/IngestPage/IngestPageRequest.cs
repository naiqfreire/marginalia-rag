namespace Marginalia.Api.Features.IngestPage;

public sealed record IngestPageRequest(string SessionId, string Url, string Title, string Text);

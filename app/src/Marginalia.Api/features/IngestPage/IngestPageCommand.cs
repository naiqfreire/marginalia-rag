using Marginalia.Api.Common;
using MediatR;

namespace Marginalia.Api.Features.IngestPage;

public sealed record IngestPageCommand(string SessionId, string Url, string Title, string Text)
    : IRequest<Result<IngestPageResponse>>;

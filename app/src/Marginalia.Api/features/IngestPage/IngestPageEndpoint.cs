using Marginalia.Api.Common;
using MediatR;

namespace Marginalia.Api.Features.IngestPage;

public static class IngestPageEndpoint
{
    public static IEndpointRouteBuilder MapIngestPage(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingest", async (
                IngestPageRequest body,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new IngestPageCommand(body.SessionId, body.Url, body.Title, body.Text),
                    ct);
                return ToHttpResult(result);
            })
            .WithName("IngestPage");

        return app;
    }

    private static IResult ToHttpResult(Result<IngestPageResponse> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Results.Ok(result.Value);
        }

        var error = result.Error ?? new Error("Unexpected failure", "INTERNAL_ERROR");
        var payload = new ErrorResponse(error.Message, error.Code);

        return error.Code switch
        {
            "VALIDATION_FAILED" => Results.BadRequest(payload),
            "TEXT_TOO_LONG" => Results.UnprocessableEntity(payload),
            _ => Results.Json(payload, statusCode: StatusCodes.Status500InternalServerError),
        };
    }
}

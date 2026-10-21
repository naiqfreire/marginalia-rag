using FluentValidation;
using Marginalia.Api.Common;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marginalia.Api.Features.IngestPage;

public sealed class IngestPageHandler : IRequestHandler<IngestPageCommand, Result<IngestPageResponse>>
{
    private readonly IValidator<IngestPageRequest> _validator;
    private readonly IngestPageLimits _limits;
    private readonly ILogger<IngestPageHandler> _logger;

    public IngestPageHandler(
        IValidator<IngestPageRequest> validator,
        IOptions<IngestPageLimits> limits,
        ILogger<IngestPageHandler> logger)
    {
        _validator = validator;
        _limits = limits.Value;
        _logger = logger;
    }

    public async Task<Result<IngestPageResponse>> Handle(
        IngestPageCommand command,
        CancellationToken cancellationToken)
    {
        var request = new IngestPageRequest(command.SessionId, command.Url, command.Title, command.Text);

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var first = validation.Errors[0];
            _logger.LogInformation(
                "IngestPage validation failed for session {SessionId}: {ErrorCode}",
                command.SessionId,
                first.ErrorCode);
            return Result<IngestPageResponse>.Fail(first.ErrorMessage, "VALIDATION_FAILED");
        }

        var words = command.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words > _limits.MaxPageWords)
        {
            _logger.LogInformation(
                "IngestPage word limit exceeded for session {SessionId}: {WordCount} words (max {MaxPageWords})",
                command.SessionId,
                words,
                _limits.MaxPageWords);
            return Result<IngestPageResponse>.Fail(
                $"Text exceeds maximum of {_limits.MaxPageWords} words",
                "TEXT_TOO_LONG");
        }

        _logger.LogInformation(
            "IngestPage accepted session {SessionId} urlLength {UrlLength} textLength {TextLength} wordCount {WordCount}",
            command.SessionId,
            command.Url.Length,
            command.Text.Length,
            words);

        return Result<IngestPageResponse>.Ok(new IngestPageResponse(command.SessionId, true));
    }
}

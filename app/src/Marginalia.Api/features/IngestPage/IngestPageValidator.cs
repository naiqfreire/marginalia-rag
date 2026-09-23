using FluentValidation;

namespace Marginalia.Api.Features.IngestPage;

public sealed class IngestPageValidator : AbstractValidator<IngestPageRequest>
{
    public IngestPageValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Text).NotEmpty();
    }
}

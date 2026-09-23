namespace Marginalia.Api.Common;

public sealed record IngestPageLimits
{
    public int MaxPageWords { get; set; } = 10_000;
}

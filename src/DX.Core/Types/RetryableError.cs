namespace DX.Core.Types;

public sealed record RetryableError(string ErrorMessage, TimeSpan RetryAfter)
    : DataSourceResult(false, ErrorMessage);
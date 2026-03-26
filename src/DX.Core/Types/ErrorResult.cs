namespace DX.Core.Types;

public sealed record ErrorResult(string ErrorMessage) : DataSourceResult(false, ErrorMessage);

namespace DX.Core.Types;

public abstract record DataSourceResult(bool IsSuccess, string? ErrorMessage = null);

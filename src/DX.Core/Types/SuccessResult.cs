namespace DX.Core.Types; 

public sealed record SuccessResult(object Data) : DataSourceResult(true);

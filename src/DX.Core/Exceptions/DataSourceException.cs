namespace DX.Core.Exceptions;

public class DataSourceException : Exception
{
    public string DataSourceName { get; }
    public DataSourceException(string dataSourceName, string message, Exception? inner = null)
        : base(message, inner) => DataSourceName = dataSourceName;
}

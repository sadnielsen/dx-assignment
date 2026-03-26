namespace DX.Core.Exceptions;

public class ConnectionFailedException : DataSourceException
{ /* ... */
    public ConnectionFailedException(string dataSourceName, string message, Exception? inner = null) : base(dataSourceName, message, inner)
    {
    }
}

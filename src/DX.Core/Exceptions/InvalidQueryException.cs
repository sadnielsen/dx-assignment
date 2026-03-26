namespace DX.Core.Exceptions;

public class InvalidQueryException : DataSourceException
{ /* ... */
    public InvalidQueryException(string dataSourceName, string message, Exception? inner = null) : base(dataSourceName, message, inner)
    {
    }
}
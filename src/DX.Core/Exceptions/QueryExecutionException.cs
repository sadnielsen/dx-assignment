namespace DX.Core.Exceptions;

public class QueryExecutionException : DataSourceException
{ /* ... */
    public QueryExecutionException(string dataSourceName, string message, Exception? inner = null) : base(dataSourceName, message, inner)
    {
    }
}

using DX.Core.Types;

namespace DX.Infrastructure.Validation;

public class SqlQueryValidator : ISqlQueryValidator
{
    private static readonly string[] AllowedQueryPrefixes = 
    [
        "SELECT",
        "INSERT",
        "UPDATE",
        "DELETE",
        "EXEC"
    ];

    public DataSourceResult Validate(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return new ErrorResult("Query cannot be null or empty.");
        }

        if (!StartsWithAllowedPrefix(query))
        {
            return new ErrorResult("Invalid query. Query must start with SELECT, INSERT, UPDATE, DELETE or EXEC.");
        }

        // [svend] : Additional validation can be added here (forbidden keywords, specific patterns for UPDATE, etc.)

        return new SuccessResult("Valid query.");
    }

    private static bool StartsWithAllowedPrefix(string query)
    {
        foreach (var prefix in AllowedQueryPrefixes)
        {
            if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

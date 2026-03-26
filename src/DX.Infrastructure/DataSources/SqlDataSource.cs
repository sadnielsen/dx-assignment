using DX.Core.Abstractions;
using DX.Core.Configuration;
using DX.Core.Exceptions;
using DX.Core.Types;
using DX.Infrastructure.Validation;

namespace DX.Infrastructure.DataSources;

public class SqlDataSource : DataSourceBase<SqlConfiguration>
{
    private readonly ISqlQueryValidator _queryValidator;

    public SqlDataSource(string name, SqlConfiguration config, ISqlQueryValidator? queryValidator = null) : base(name, config)
    {
        _queryValidator = queryValidator ?? new SqlQueryValidator();
    }

    // Implementeer ConnectCoreAsync
    //   - Simuleer verbinding (Task.Delay)
    //   - Valideer ConnectionString is niet leeg
    //   - Return SuccessResult met connectie-info
    protected override async Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
    {
        var connString = Configuration.ConnectionString;
        if (string.IsNullOrEmpty(connString))
        {
            throw new Exception("Connection string cannot be null or empty.");
        }

        await Task.Delay(500, ct);
        return new SuccessResult($"Connected to {connString}");
    }

    // TODO: Implementeer DisconnectCoreAsync
    //   - Log SQL-specifieke cleanup info
    protected override async Task DisconnectCoreAsync(CancellationToken ct)
    {
        
    }

    // Implementeer GetDataCoreAsync
    //   - Valideer query: moet beginnen met SELECT, INSERT, UPDATE, DELETE of EXEC
    //   - Gooi InvalidQueryException voor ongeldige queries
    //   - Return mock data als SuccessResult
    //   - Simuleer timeout als query "slow" bevat (return RetryableError)
    protected override async Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.");
        }

        // [svend] validation separate for readability, responsibility and easier testing. 
        var validationResult = _queryValidator.Validate(query);

        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        if(query.Contains("slow", StringComparison.OrdinalIgnoreCase))
        {
            throw new QueryExecutionException(Name, "Query excecution timed out.");
        }

        return new SuccessResult("Query executed successfully.");
    }

    public override string GetReportInfo()
    {
        return $"Type: SQL\n  Connection: {Configuration.ConnectionString}";
    }
}

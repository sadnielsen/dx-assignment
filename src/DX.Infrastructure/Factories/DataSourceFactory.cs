using DX.Core.Configuration;
using DX.Core.Interfaces;
using DX.Infrastructure.DataSources;
using DX.Infrastructure.Validation;

namespace DX.Infrastructure.Factories;

/// <summary>
/// Concrete factory for creating data source instances.
/// Lives in Infrastructure layer to avoid Application depending on Infrastructure.
/// </summary>
public class DataSourceFactory : IDataSourceFactory
{
    private readonly ISqlQueryValidator? _sqlQueryValidator;

    public DataSourceFactory(ISqlQueryValidator? sqlQueryValidator = null)
    {
        _sqlQueryValidator = sqlQueryValidator;
    }

    public IDataSource CreateSqlDataSource(string name, SqlConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new SqlDataSource(name, config, _sqlQueryValidator);
    }

    public IDataSource CreateApiDataSource(string name, ApiConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new ApiDataSource(name, config);
    }

    public IDataSource CreateFileDataSource(string name, FileConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new FileDataSource(name, config);
    }
}

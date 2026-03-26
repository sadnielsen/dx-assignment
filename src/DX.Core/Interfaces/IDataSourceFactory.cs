using DX.Core.Configuration;

namespace DX.Core.Interfaces;

/// <summary>
/// Abstract factory interface for creating data sources.
/// Implementation should be in Infrastructure layer.
/// </summary>
public interface IDataSourceFactory
{
    IDataSource CreateSqlDataSource(string name, SqlConfiguration config);
    IDataSource CreateApiDataSource(string name, ApiConfiguration config);
    IDataSource CreateFileDataSource(string name, FileConfiguration config);
}

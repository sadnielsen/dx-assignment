using DX.Core.Configuration;
using DX.Core.Interfaces;

namespace DX.Application.Services;

/// <summary>
/// Application service that combines factory and registration.
/// Delegates creation to IDataSourceFactory and handles registration.
/// </summary>
public class DataSourceService
{
    private readonly DataSourceManager _manager;
    private readonly IDataSourceFactory _factory;

    public DataSourceService(DataSourceManager manager, IDataSourceFactory factory)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IDataSource CreateAndRegisterSqlDataSource(string name, SqlConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var dataSource = _factory.CreateSqlDataSource(name, config);
        _manager.Register(dataSource);
        return dataSource;
    }

    public IDataSource CreateAndRegisterApiDataSource(string name, ApiConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var dataSource = _factory.CreateApiDataSource(name, config);
        _manager.Register(dataSource);
        return dataSource;
    }

    public IDataSource CreateAndRegisterFileDataSource(string name, FileConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var dataSource = _factory.CreateFileDataSource(name, config);
        _manager.Register(dataSource);
        return dataSource;
    }
}

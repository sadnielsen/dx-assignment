using DX.Core.Types;

namespace DX.Core.Interfaces;

public interface IDataSource : IAsyncDisposable
{
    string Name { get; }
    DataSourceState State { get; }
    Task<DataSourceResult> ConnectAsync(CancellationToken ct = default);
    Task<DataSourceResult> GetDataAsync(string query, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    string GetReportInfo();
}
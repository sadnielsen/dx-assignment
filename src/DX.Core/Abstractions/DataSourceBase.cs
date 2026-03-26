using DX.Core.Types;
using DX.Core.Exceptions;
using DX.Core.Interfaces;

namespace DX.Core.Abstractions;

public abstract class DataSourceBase<TConfig> : IDataSource
    where TConfig : class
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    protected DataSourceBase(string name, TConfig configuration)
    {
        Name = name;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; }
    public DateTime CreatedAt { get; }
    public DataSourceState State { get; protected set; }
    protected TConfig Configuration { get; }

    // Template method: subclasses implementeren deze
    protected abstract Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct);
    protected abstract Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct);
    protected abstract Task DisconnectCoreAsync(CancellationToken ct);

    public abstract string GetReportInfo();

    public async Task<DataSourceResult> ConnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await EnsureConnectedAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    // Implementeer GetDataAsync
    //   - Gebruik _lock voor thread-safety
    //   - Auto-connect als State != Connected
    //   - Valideer dat query niet null/empty is
    //   - Delegeer naar GetDataCoreAsync
    //   - Wrap exceptions in QueryExecutionException
    public async Task<DataSourceResult> GetDataAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        await _lock.WaitAsync(ct);
        try
        {
            var connectResult = await EnsureConnectedAsync(ct);
            if (!connectResult.IsSuccess)
            {
                return connectResult;
            }

            try
            {
                return await GetDataCoreAsync(query, ct);
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new QueryExecutionException(Name, $"Failed to execute query on {Name}:{ex.Message}.", ex);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // Implementeer DisconnectAsync
    //   - Gebruik _lock voor thread-safety
    //   - Alleen disconnecten als State == Connected
    //   - Delegeer naar DisconnectCoreAsync
    //   - Zet State naar Disconnected
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);

        try
        {
            if (State != DataSourceState.Connected)
            {
                return;
            }

            await DisconnectCoreAsync(ct);
            State = DataSourceState.Disconnected;
        }
        catch
        {
            State = DataSourceState.Error;
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    // Implementeer DisposeAsync
    //   - Disconnect als verbonden
    //   - Dispose _lock
    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            await DisconnectAsync(); // [svend] keep logic in DisconnectAsync
        }
        finally
        {
            _lock.Dispose();
        }
    }

    /// <summary>
    /// [svend] Helper method for connection logic and state. Assumes lock has been applied.
    /// </summary>
    private async Task<DataSourceResult> EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (State == DataSourceState.Connected)
        {
            return new SuccessResult($"Already connected to {Name}");
        }

        try
        {
            State = DataSourceState.Connecting;

            var result = await ConnectCoreAsync(ct);
            State = result.IsSuccess
                ? DataSourceState.Connected
                : DataSourceState.Error;

            return result;
        }
        catch (Exception ex)
        {
            State = DataSourceState.Error;
            throw new ConnectionFailedException(Name, $"Connection failed for {Name}", ex);
        }
    }
}

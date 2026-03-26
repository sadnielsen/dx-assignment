using DX.Core.Interfaces;
using DX.Core.Types;
using System.Text;

namespace DX.Application.Services;

public class DataSourceManager : IAsyncDisposable
{
    private readonly List<IDataSource> _dataSources = [];

    public void Register(IDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        if (_dataSources.Any(ds => ds.Name == dataSource.Name))
            throw new InvalidOperationException($"Data source '{dataSource.Name}' already registered");
        _dataSources.Add(dataSource);
    }


    // Implementeer ExecuteOnAllAsync
    //   - Voer query parallel uit op alle bronnen (Task.WhenAll)
    //   - Gebruik CancellationToken
    //   - Return Dictionary<string, DataSourceResult> (name -> result)
    //   - Vang per-bron exceptions op en map naar ErrorResult
    // [svend] TODO: define some dto instead of tuple.
    public async Task<Dictionary<string, DataSourceResult>> ExecuteOnAllAsync(
    string query,
    CancellationToken cancellationToken)
    {
        var tasks = _dataSources
            .Select(ds => ExecuteOnSingleAsync(ds, query, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(x => x.Name, x => x.Result);
    }

    /// <summary>
    /// Executes the provided query on a single data source and returns the result.
    /// Re-throws <see cref="OperationCanceledException"/> when cancellation is requested.
    /// </summary>
    private async Task<(string Name, DataSourceResult Result)> ExecuteOnSingleAsync(
        IDataSource ds,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ds.GetDataAsync(query, cancellationToken);
            return (ds.Name, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error querying '{ds.Name}': {ex.Message}");
            return (ds.Name, new ErrorResult(ex.Message));
        }
    }

    // Implementeer GetByType<T>() where T : IDataSource
    //   - Return IReadOnlyList<T> van bronnen van type T
    //   - Gebruik OfType<T>()
    public IReadOnlyList<T> GetByType<T>() where T : IDataSource
    {
        return [.. _dataSources.OfType<T>()];
    }

    /// <summary>
    /// Gets all registered data sources.
    /// </summary>
    public IReadOnlyList<IDataSource> GetAll()
    {
        return _dataSources.AsReadOnly();
    }

    // Implementeer GenerateReportAsync
    //   - Genereer een rapport string met info over alle bronnen
    //   - Gebruik StringBuilder voor performance
    public string GenerateReportAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Data Source Report:");
        foreach (var ds in _dataSources)
        {
            sb.AppendLine($"- Name: {ds.Name}");
            sb.AppendLine($"  {ds.GetReportInfo()}");
        }
        return sb.ToString();
    }

    // Implementeer DisposeAsync
    //   - Dispose alle geregistreerde bronnen
    //   - Vang en log individuele disposal fouten
    //   - Clear de lijst
    public async ValueTask DisposeAsync()
    {
        foreach (var ds in _dataSources)
        {
            try
            {
                await ds.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Log exception (placeholder)
                Console.Error.WriteLine($"Error disposing '{ds.Name}': {ex.Message}");
            }
        }

        _dataSources.Clear();
    }
}

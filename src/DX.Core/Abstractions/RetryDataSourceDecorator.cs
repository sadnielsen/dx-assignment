using DX.Core.Interfaces;
using DX.Core.Types;

namespace DX.Core.Abstractions;

public class RetryDataSourceDecorator : IDataSource
{
    private readonly IDataSource _inner;
    private readonly int _maxRetries;
    private readonly ILogger? _logger;

    public RetryDataSourceDecorator(IDataSource inner, int maxRetries = 3, ILogger? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _maxRetries = maxRetries;
        _logger = logger;
    }

    public string Name => _inner.Name;
    public DataSourceState State => _inner.State;

    public Task<DataSourceResult> ConnectAsync(CancellationToken ct = default)
        => _inner.ConnectAsync(ct);

    public Task DisconnectAsync(CancellationToken ct = default)
        => _inner.DisconnectAsync(ct);

    public async Task<DataSourceResult> GetDataAsync(string query, CancellationToken ct = default)
    {
        DataSourceResult lastResult = new ErrorResult("No attempts made");
        int attempt = 0;

        while (attempt < _maxRetries)
        {
            attempt++;
            Console.WriteLine($"[Retry] Attempt {attempt}/{_maxRetries} for {Name}");

            try
            {
                lastResult = await _inner.GetDataAsync(query, ct);

                // Success - return immediately
                if (lastResult is SuccessResult)
                {
                    return lastResult;
                }

                // Non-retryable error - return immediately
                if (lastResult is ErrorResult)
                {
                    return lastResult;
                }

                // Retryable error - wait and retry
                if (lastResult is RetryableError retryable)
                {
                    if (attempt < _maxRetries)
                    {
                        Console.WriteLine($"[Retry] Retryable error. Waiting {retryable.RetryAfter.TotalSeconds}s before retry...");
                        await Task.Delay(retryable.RetryAfter, ct);
                    }
                    continue;
                }

                // Unknown result type - treat as non-retryable
                return lastResult;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Log and return error (don't retry exceptions)
                _logger?.LogError($"[Retry] Exception on attempt {attempt}: {ex.Message}");
                return new ErrorResult($"Exception during query execution: {ex.Message}");
            }
        }

        // Max retries reached
        _logger?.LogWarning($"[Retry] Max retries ({_maxRetries}) reached for {Name}");
        return lastResult;
    }

    public ValueTask DisposeAsync()
        => _inner.DisposeAsync();

    public string GetReportInfo()
        => _inner.GetReportInfo();
}

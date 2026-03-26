using System.Text.Json;
using DX.Core.Abstractions;
using DX.Core.Configuration;
using DX.Core.Exceptions;
using DX.Core.Types;

namespace DX.Infrastructure.DataSources;

public class ApiDataSource : DataSourceBase<ApiConfiguration>
{
    private record ApiMockData(string Endpoint, List<ApiMockItem> Data);
    private record ApiMockItem(int Id, string Value);

    public ApiDataSource(string name, ApiConfiguration config) : base(name, config) { }

    public void AddHeader(string key, string value) { /* ... */ }

    // Implementeer ConnectCoreAsync
    //   - Valideer BaseUrl formaat (moet geldig URI zijn)
    //   - Simuleer health check
    //   - Return resultaat
    protected override async Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
    {
        try
        {
            var uri = new Uri(Configuration.BaseUrl);

            // Check uri
            bool isHealthy = true;
            if (isHealthy)
            {
                return new SuccessResult("Connected to API.");
            }
            else
            {
                return new ErrorResult("API connection failed.");
            }
        }
        catch (UriFormatException)
        {
            throw new UriFormatException($"Invalid BaseUrl format: {Configuration.BaseUrl}");
        }
    }

    protected override async Task DisconnectCoreAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    // Implementeer GetDataCoreAsync
    //   - Behandel query als endpoint path (bv. "/users", "/orders/123")
    //   - Valideer dat path begint met "/"
    //   - Return mock JSON data als string in SuccessResult
    //   - Simuleer 404 als path "/notfound" bevat (return ErrorResult)
    //   - Simuleer rate limit als path "/ratelimit" bevat (return RetryableError)
    protected override async Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
    {
        if (!query.StartsWith("/"))
        {
            throw new InvalidQueryException(Name, "API path must start with '/'");
        }

        if (query.Contains("/notfound"))
        {
            return new ErrorResult($"API endpoint not found: {query}");
        }

        if (query.Contains("/ratelimit"))
        {
            return new RetryableError("API rate limit exceeded", TimeSpan.FromSeconds(60));
        }

        var mockData = new ApiMockData(
            query,
            [new ApiMockItem(1, "mock")]
        );
        var json = JsonSerializer.Serialize(mockData);
        return new SuccessResult(json);
    }

    public override string GetReportInfo()
    {
        return $"Type: API\n  BaseUrl: {Configuration.BaseUrl}";
    }
}

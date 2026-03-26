using System.Diagnostics;
using DX.Application.Services;
using DX.Cli.UI;
using DX.Core.Abstractions;
using DX.Core.Interfaces;

namespace DX.Cli.Commands;

public class CommandHandler
{
    private readonly DataSourceManager _manager;
    private readonly DataSourceService _service;

    public CommandHandler(DataSourceManager manager, DataSourceService service)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async Task HandleTestConnectionAsync(CancellationToken ct = default)
    {
        var dataSources = _manager.GetAll();
        if (dataSources.Count == 0)
        {
            MenuRenderer.ShowError("Geen data sources beschikbaar. Voeg eerst een data source toe.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Beschikbare data sources:");
        for (int i = 0; i < dataSources.Count; i++)
        {
            var ds = dataSources[i];
            Console.WriteLine($"  {i + 1}. {ds.Name} (State: {ds.State})");
        }

        var selection = UserInputHelper.ReadInt("Selecteer data source", 1, dataSources.Count, 1);
        var dataSource = dataSources[selection - 1];

        Console.WriteLine();
        MenuRenderer.ShowInfo($"Verbinding maken met '{dataSource.Name}'...");

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await dataSource.ConnectAsync(ct);
            sw.Stop();

            ResultFormatter.FormatSingleResult(dataSource.Name, result, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            MenuRenderer.ShowError($"Exception: {ex.Message}");
        }
    }

    public async Task HandleQueryAllAsync(CancellationToken ct = default)
    {
        var dataSources = _manager.GetAll();
        if (dataSources.Count == 0)
        {
            MenuRenderer.ShowError("Geen data sources beschikbaar. Voeg eerst een data source toe.");
            return;
        }

        Console.WriteLine();
        var defaultQuery = GetDefaultQuery();
        var query = UserInputHelper.ReadNonEmptyString("Voer query in", defaultQuery);

        Console.WriteLine();
        MenuRenderer.ShowInfo($"Query uitvoeren op {dataSources.Count} data source(s)...");

        var stopwatches = new Dictionary<string, Stopwatch>();
        foreach (var ds in dataSources)
        {
            stopwatches[ds.Name] = Stopwatch.StartNew();
        }

        var results = await _manager.ExecuteOnAllAsync(query, ct);

        foreach (var (name, sw) in stopwatches)
        {
            sw.Stop();
        }

        var resultsWithTiming = results.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value, stopwatches[kvp.Key].ElapsedMilliseconds)
        );

        ResultFormatter.FormatResults(resultsWithTiming);
    }

    public async Task HandleQueryWithRetryAsync(CancellationToken ct = default)
    {
        var dataSources = _manager.GetAll();
        if (dataSources.Count == 0)
        {
            MenuRenderer.ShowError("Geen data sources beschikbaar. Voeg eerst een data source toe.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Beschikbare data sources:");
        for (int i = 0; i < dataSources.Count; i++)
        {
            var ds = dataSources[i];
            Console.WriteLine($"  {i + 1}. {ds.Name} (State: {ds.State})");
        }

        var selection = UserInputHelper.ReadInt("Selecteer data source", 1, dataSources.Count, 1);
        var dataSource = dataSources[selection - 1];

        var defaultQuery = GetDefaultQueryForSource(dataSource);
        var query = UserInputHelper.ReadNonEmptyString("Voer query in", defaultQuery);
        var maxRetries = UserInputHelper.ReadInt("Aantal retries", 1, 10, 3);

        Console.WriteLine();
        MenuRenderer.ShowInfo($"Query uitvoeren met retry decorator (max {maxRetries} retries)...");
        Console.WriteLine();

        var retryDecorator = new RetryDataSourceDecorator(dataSource, maxRetries);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await retryDecorator.GetDataAsync(query, ct);
            sw.Stop();

            ResultFormatter.FormatSingleResult(dataSource.Name, result, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            MenuRenderer.ShowError($"Exception na alle retries: {ex.Message}");
        }
    }

    public Task HandleShowReportAsync()
    {
        var dataSources = _manager.GetAll();
        if (dataSources.Count == 0)
        {
            MenuRenderer.ShowError("Geen data sources beschikbaar. Voeg eerst een data source toe.");
            return Task.CompletedTask;
        }

        MenuRenderer.ShowInfo("Rapport genereren...");
        var report = _manager.GenerateReportAsync();
        ResultFormatter.FormatReport(report);
        return Task.CompletedTask;
    }

    private string GetDefaultQuery()
    {
        var dataSources = _manager.GetAll();
        if (dataSources.Count == 0)
            return "SELECT * FROM Users";

        return GetDefaultQueryForSource(dataSources[0]);
    }

    private string GetDefaultQueryForSource(IDataSource dataSource)
    {
        //return dataSource switch
        //{
        //    SqlDataSource => "SELECT * FROM Users",
        //    ApiDataSource => "/users",
        //    FileDataSource => "header",
        //    _ => "test query"
        //};
        return "test query";
    }
}

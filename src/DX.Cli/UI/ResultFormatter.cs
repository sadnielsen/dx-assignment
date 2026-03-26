using System.Diagnostics;
using System.Text;
using DX.Core.Interfaces;
using DX.Core.Types;
using DX.Infrastructure.DataSources;

namespace DX.Cli.UI;

public static class ResultFormatter
{
    public static void FormatResults(Dictionary<string, (DataSourceResult Result, long ElapsedMs)> results)
    {
        if (results.Count == 0)
        {
            MenuRenderer.ShowInfo("Geen data sources beschikbaar.");
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- Resultaten ------------------------");
        Console.ResetColor();

        int successCount = 0;
        int errorCount = 0;

        foreach (var (name, (result, elapsed)) in results.OrderBy(x => x.Key))
        {
            var typeIcon = GetTypeIcon(name);
            var statusIcon = result.IsSuccess ? "OK" : "FAIL";
            var statusColor = result.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red;
            var statusText = GetStatusText(result);

            Console.Write($"[{typeIcon}]  ");
            Console.Write($"{name,-20}");

            Console.ForegroundColor = statusColor;
            Console.Write($"[{statusIcon}] {statusText,-12}");
            Console.ResetColor();

            Console.WriteLine($"({elapsed}ms)");

            if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"       -> {result.ErrorMessage}");
                Console.ResetColor();
            }

            if (result.IsSuccess) successCount++;
            else errorCount++;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("---------------------------------------");
        Console.ResetColor();
        Console.WriteLine($"Totaal: {successCount} succes, {errorCount} fout");
        Console.WriteLine();
    }

    public static void FormatSingleResult(string dataSourceName, DataSourceResult result, long elapsedMs)
    {
        Console.WriteLine();
        var typeIcon = GetTypeIcon(dataSourceName);
        Console.Write($"[{typeIcon}] {dataSourceName}: ");

        if (result.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Succes ({elapsedMs}ms)");
            Console.ResetColor();

            if (result is SuccessResult success && success.Data is string data)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Data: {data}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {GetStatusText(result)} ({elapsedMs}ms)");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            Console.ResetColor();
        }
    }

    public static void FormatReport(string report)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- Data Source Rapport --------------");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine(report);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string GetTypeIcon(string name)
    {
        if (name.Contains("SQL", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Database", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("DB", StringComparison.OrdinalIgnoreCase))
            return "SQL";

        if (name.Contains("API", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("REST", StringComparison.OrdinalIgnoreCase))
            return "API";

        if (name.Contains("File", StringComparison.OrdinalIgnoreCase) ||
            name.Contains(".json", StringComparison.OrdinalIgnoreCase) ||
            name.Contains(".xml", StringComparison.OrdinalIgnoreCase) ||
            name.Contains(".csv", StringComparison.OrdinalIgnoreCase))
            return "File";

        return "???";
    }

    private static string GetStatusText(DataSourceResult result)
    {
        return result switch
        {
            SuccessResult => "Succes",
            RetryableError retry => $"Retry ({retry.RetryAfter.TotalSeconds}s)",
            ErrorResult => "Error",
            _ => "Onbekend"
        };
    }
}

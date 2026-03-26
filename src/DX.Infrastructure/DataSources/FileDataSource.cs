using DX.Core.Abstractions;
using DX.Core.Configuration;
using DX.Core.Types;

namespace DX.Infrastructure.DataSources;

public class FileDataSource : DataSourceBase<FileConfiguration>
{
    public FileDataSource(string name, FileConfiguration config) : base(name, config) { }

    // Implementeer ConnectCoreAsync
    //   - Controleer of FilePath een geldig pad is
    //   - Simuleer bestandstoegang check
    //   - Return resultaat
    protected override async Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Configuration.FilePath))
        {
            return new ErrorResult("FilePath is invalid.");
        }
        // Simuleer bestandstoegang check (in een echte implementatie zou je hier een file handle openen)
        bool fileAccessible = true; // Simuleer dat het bestand toegankelijk is
        if (!fileAccessible)
        {
            return new ErrorResult("Unable to access the file.");
        }

        return new SuccessResult($"Connected successfully to source {Configuration.FilePath}.");
    }

    // TODO: Implementeer DisconnectCoreAsync
    //   - Log bestand-specifieke cleanup
    //   - Simuleer file handle release
    protected override async Task DisconnectCoreAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
    }


    // Implementeer GetDataCoreAsync
    //   - Behandel query als sectie/filter (bv. "header", "line:5", "search:keyword")
    //   - Parseer het query formaat en return mock data
    //   - Return ErrorResult voor onbekend query formaat
    protected override async Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
    {
        if (query.Equals("header", StringComparison.OrdinalIgnoreCase))
        {
            var mockHeader = new { FileName = Configuration.FilePath, Size = "1024 KB", LastModified = DateTime.UtcNow };
            return new SuccessResult(mockHeader);
        }

        if (query.StartsWith("line:", StringComparison.OrdinalIgnoreCase))
        {
            var lineNumberStr = query.Substring(5);
            
            if (int.TryParse(lineNumberStr, out int lineNumber))
            {
                var mockLine = $"This is contents of linenumber {lineNumber}";
                return new SuccessResult(mockLine);
            }

            return new ErrorResult("Invalid line number format.");
        }

        if (query.StartsWith("search:", StringComparison.OrdinalIgnoreCase))
        {
            var keyword = query.Substring(7);
            var mockSearchResults = new List<string>
            {
                $"Line 10: Found '{keyword}' in this line",
                $"Line 25: Another occurrence of '{keyword}'"
            };
            return new SuccessResult(mockSearchResults);
        }

        return new ErrorResult($"Unknown query format: '{query}'. Expected 'header', 'line:<number>', or 'search:<keyword>'.");
    }

    public override string GetReportInfo()
    {
        return $"Type: FILE\n  Path: {Configuration.FilePath}";
    }
}

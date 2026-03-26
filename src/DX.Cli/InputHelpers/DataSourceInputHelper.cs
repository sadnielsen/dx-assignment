using DX.Cli.UI;
using DX.Core.Configuration;

namespace DX.Cli.InputHelpers;

public static class DataSourceInputHelper
{
    private static int _sqlCounter = 0;
    private static int _apiCounter = 0;
    private static int _fileCounter = 0;

    public static (string Name, SqlConfiguration Config) GatherSqlInput()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- SQL Data Source Toevoegen ---------");
        Console.ResetColor();
        Console.WriteLine();

        _sqlCounter++;
        var defaultName = $"SQL Database {_sqlCounter}";
        var defaultConnectionString = "Server=localhost;Database=DXMS;Integrated Security=true;";
        var defaultTimeout = 30;

        var name = UserInputHelper.ReadNonEmptyString("Voer naam in", defaultName);
        var connectionString = UserInputHelper.ReadNonEmptyString("Voer connection string in", defaultConnectionString);
        var timeout = UserInputHelper.ReadInt("Command timeout in seconden", 1, 300, defaultTimeout);

        var config = new SqlConfiguration(connectionString, timeout);
        return (name, config);
    }

    public static (string Name, ApiConfiguration Config) GatherApiInput()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- API Data Source Toevoegen ---------");
        Console.ResetColor();
        Console.WriteLine();

        _apiCounter++;
        var defaultName = $"API Service {_apiCounter}";
        var defaultBaseUrl = "https://api.example.com";

        var name = UserInputHelper.ReadNonEmptyString("Voer naam in", defaultName);
        var baseUrl = UserInputHelper.ReadNonEmptyString("Voer base URL in", defaultBaseUrl);

        Dictionary<string, string>? headers = null;
        if (UserInputHelper.ReadYesNo("Wil je custom headers toevoegen?", false))
        {
            headers = new Dictionary<string, string>();
            while (true)
            {
                var headerName = UserInputHelper.ReadString("Header naam (leeg = klaar)");
                if (string.IsNullOrWhiteSpace(headerName))
                    break;

                var defaultHeaderValue = headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    ? "Bearer token123"
                    : "value";
                var headerValue = UserInputHelper.ReadNonEmptyString("Header waarde", defaultHeaderValue);
                headers[headerName] = headerValue;
            }
        }

        var config = new ApiConfiguration(baseUrl, headers);
        return (name, config);
    }

    public static (string Name, FileConfiguration Config) GatherFileInput()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- File Data Source Toevoegen --------");
        Console.ResetColor();
        Console.WriteLine();

        _fileCounter++;
        var defaultName = $"File Source {_fileCounter}";
        var defaultFilePath = $"data{_fileCounter}.json";

        var name = UserInputHelper.ReadNonEmptyString("Voer naam in", defaultName);
        var filePath = UserInputHelper.ReadNonEmptyString("Voer bestandspad in", defaultFilePath);
        var readOnly = !UserInputHelper.ReadYesNo("Schrijftoegang toestaan?", false);

        var config = new FileConfiguration(filePath, readOnly);
        return (name, config);
    }
}



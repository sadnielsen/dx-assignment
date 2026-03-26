using DX.Application.Factories;
using DX.Application.Services;
using DX.Cli.Commands;
using DX.Cli.InputHelpers;
using DX.Cli.UI;
using DX.Core.Interfaces;
using DX.Infrastructure.Factories;
using DX.Infrastructure.Logging;

MenuRenderer.ShowHeader();
MenuRenderer.ShowInfo("DXMS Data Source Demo - Polymorfisme in actie!");
MenuRenderer.ShowInfo("Deze applicatie demonstreert het Result pattern, async programming en resource management.");
UserInputHelper.WaitForKey();

// Setup services with proper DI
await using var manager = new DataSourceManager();
IDataSourceFactory factory = new DX.Infrastructure.Factories.DataSourceFactory();
var service = new DX.Application.Factories.DataSourceService(manager, factory);
var logger = new ConsoleLogger();
var commandHandler = new CommandHandler(manager, service);
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    MenuRenderer.ShowInfo("Afsluiten aangevraagd...");
};

bool running = true;

while (running && !cts.Token.IsCancellationRequested)
{
    try
    {
        MenuRenderer.ShowHeader();
        MenuRenderer.ShowMenu();

        var choice = UserInputHelper.ReadInt("Selecteer optie", 1, 8);

        switch (choice)
        {
            case 1:
                try
                {
                    var (name, config) = DataSourceInputHelper.GatherSqlInput();
                    service.CreateAndRegisterSqlDataSource(name, config);
                    MenuRenderer.ShowSuccess($"SQL Data Source '{name}' toegevoegd");
                }
                catch (Exception ex)
                {
                    MenuRenderer.ShowError($"Fout bij toevoegen: {ex.Message}");
                }
                UserInputHelper.WaitForKey();
                break;

            case 2:
                try
                {
                    var (name, config) = DataSourceInputHelper.GatherApiInput();
                    service.CreateAndRegisterApiDataSource(name, config);
                    MenuRenderer.ShowSuccess($"API Data Source '{name}' toegevoegd");
                }
                catch (Exception ex)
                {
                    MenuRenderer.ShowError($"Fout bij toevoegen: {ex.Message}");
                }
                UserInputHelper.WaitForKey();
                break;

            case 3:
                try
                {
                    var (name, config) = DataSourceInputHelper.GatherFileInput();
                    service.CreateAndRegisterFileDataSource(name, config);
                    MenuRenderer.ShowSuccess($"File Data Source '{name}' toegevoegd");
                }
                catch (Exception ex)
                {
                    MenuRenderer.ShowError($"Fout bij toevoegen: {ex.Message}");
                }
                UserInputHelper.WaitForKey();
                break;

            case 4:
                await commandHandler.HandleTestConnectionAsync(cts.Token);
                UserInputHelper.WaitForKey();
                break;

            case 5:
                await commandHandler.HandleQueryAllAsync(cts.Token);
                UserInputHelper.WaitForKey();
                break;

            case 6:
                await commandHandler.HandleQueryWithRetryAsync(cts.Token);
                UserInputHelper.WaitForKey();
                break;

            case 7:
                await commandHandler.HandleShowReportAsync();
                UserInputHelper.WaitForKey();
                break;

            case 8:
                running = false;
                MenuRenderer.ShowInfo("Applicatie wordt afgesloten...");
                MenuRenderer.ShowInfo("Data sources worden opgeruimd (await using)...");
                break;
        }
    }
    catch (OperationCanceledException)
    {
        running = false;
        MenuRenderer.ShowInfo("Operatie geannuleerd.");
    }
    catch (Exception ex)
    {
        MenuRenderer.ShowError($"Onverwachte fout: {ex.Message}");
        UserInputHelper.WaitForKey();
    }
}

Console.WriteLine();
MenuRenderer.ShowSuccess("Applicatie afgesloten. Tot ziens!");
Console.WriteLine();

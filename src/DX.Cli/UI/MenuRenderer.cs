namespace DX.Cli.UI;

public static class MenuRenderer
{
    public static void ShowHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("+========================================+");
        Console.WriteLine("|    DXMS Data Source Demo               |");
        Console.WriteLine("+========================================+");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void ShowMenu()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("+========================================+");
        Console.WriteLine("|  1. SQL Data Source toevoegen          |");
        Console.WriteLine("|  2. API Data Source toevoegen          |");
        Console.WriteLine("|  3. File Data Source toevoegen         |");
        Console.WriteLine("|  4. Verbinding testen                  |");
        Console.WriteLine("|  5. Query uitvoeren op alle bronnen    |");
        Console.WriteLine("|  6. Query met retry uitvoeren          |");
        Console.WriteLine("|  7. Rapport tonen                      |");
        Console.WriteLine("|  8. Afsluiten                          |");
        Console.WriteLine("+========================================+");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }
}

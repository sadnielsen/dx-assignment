using DX.Core.Interfaces;

namespace DX.Infrastructure.Logging;

public class ConsoleLogger : ILogger
{
    public void LogInformation(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARN] {message}");
        Console.ResetColor();
    }

    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public void LogDebug(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }
}

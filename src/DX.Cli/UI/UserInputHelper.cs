namespace DX.Cli.UI;

public static class UserInputHelper
{
    public static string ReadString(string prompt, string? defaultValue = null)
    {
        if (defaultValue != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{prompt} [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(defaultValue);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]: ");
            Console.ResetColor();
        }
        else
        {
            Console.Write($"{prompt}: ");
        }

        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) && defaultValue != null ? defaultValue : input ?? string.Empty;
    }

    public static string ReadNonEmptyString(string prompt, string? defaultValue = null)
    {
        while (true)
        {
            var input = ReadString(prompt, defaultValue);
            if (!string.IsNullOrWhiteSpace(input))
                return input;

            MenuRenderer.ShowError("Invoer mag niet leeg zijn. Probeer opnieuw.");
        }
    }

    public static int ReadInt(string prompt, int min, int max, int? defaultValue = null)
    {
        while (true)
        {
            if (defaultValue.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{prompt} ({min}-{max}) [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(defaultValue.Value);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("]: ");
                Console.ResetColor();
            }
            else
            {
                Console.Write($"{prompt} ({min}-{max}): ");
            }

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) && defaultValue.HasValue)
                return defaultValue.Value;

            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;

            MenuRenderer.ShowError($"Voer een geldig getal in tussen {min} en {max}.");
        }
    }

    public static bool ReadYesNo(string prompt, bool? defaultValue = null)
    {
        while (true)
        {
            if (defaultValue.HasValue)
            {
                var defaultChar = defaultValue.Value ? "j" : "n";
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{prompt} (j/n) [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(defaultChar);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("]: ");
                Console.ResetColor();
            }
            else
            {
                Console.Write($"{prompt} (j/n): ");
            }

            var input = Console.ReadLine()?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(input) && defaultValue.HasValue)
                return defaultValue.Value;

            if (input == "j" || input == "ja" || input == "y" || input == "yes")
                return true;
            if (input == "n" || input == "nee" || input == "no")
                return false;

            MenuRenderer.ShowError("Voer 'j' of 'n' in.");
        }
    }

    public static void WaitForKey(string message = "Druk op een toets om door te gaan...")
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.ReadKey(true);
    }
}

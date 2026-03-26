namespace DX.Core.Interfaces;

public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}

using SharpGuard.Core.Configuration;

namespace SharpGuard.Core.Services;

/// <summary>
/// Simple logger abstraction
/// </summary>
public interface ILogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception? exception, string message, params object[] args);
    void LogDebug(string message, params object[] args);
}

/// <summary>
/// Console-based logger implementation
/// Implements Observer pattern
/// </summary>
public class ConsoleLogger(
    LogLevel minimumLevel = LogLevel.Information
) : ILogger
{
    public void LogInformation(string message, params object[] args)
    {
        if (minimumLevel >= LogLevel.Information)
        {
            Console.WriteLine($"[INFO] {FormatMessage(message, args)}");
        }
    }

    public void LogWarning(string message, params object[] args)
    {
        if (minimumLevel >= LogLevel.Warning)
        {
            Console.WriteLine($"[WARN] {FormatMessage(message, args)}");
        }
    }

    public void LogError(Exception? exception, string message, params object[] args)
    {
        if (minimumLevel >= LogLevel.Error)
        {
            Console.WriteLine($"[ERROR] {FormatMessage(message, args)}");
            if (exception != null)
            {
                Console.WriteLine($"       {exception}");
            }
        }
    }

    public void LogDebug(string message, params object[] args)
    {
        if (minimumLevel >= LogLevel.Debug)
        {
            Console.WriteLine($"[DEBUG] {FormatMessage(message, args)}");
        }
    }

    private static string FormatMessage(string message, object[] args)
    {
        try
        {
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        catch
        {
            return message;
        }
    }
}

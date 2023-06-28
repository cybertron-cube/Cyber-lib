namespace Cybertron.CLogger;

public abstract class BaseLogger
{
    public event Action<string, LogLevel>? OnLog;
    public abstract LogLevel DefaultLogLevel { get; set; }
    private protected readonly object _lock = new();
    public void Log(string message)
    {
        Log(message, DefaultLogLevel);
    }
    public void LogError(string message)
    {
        Log(message, LogLevel.Error);
    }
    public void LogWarning(string message)
    {
        Log(message, LogLevel.Warning);
    }
    public void Log(string message, LogLevel level)
    {
        lock (_lock)
        {
            CustomLog(message, level);
            OnLog?.Invoke(message, level);
        }
    }
    private protected abstract void CustomLog(string message, LogLevel level);
}

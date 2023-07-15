namespace Cybertron.CLogger;

public interface ILogger
{
    string FilePath { get; }
    LogLevel DefaultLogLevel { get; set; }
    void Log(string message);
    void Log(string message, LogLevel level);
}

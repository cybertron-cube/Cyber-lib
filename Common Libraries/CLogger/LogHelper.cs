namespace Cybertron.CLogger;

public static class LogHelper
{
    public static ILogger GetLogger(LogType logType, string filePath)
    {
        return logType switch
        {
            LogType.SingleFileLog => new SingleFileLog(filePath),
            LogType.MultiFileLog => new MultiFileLogger(filePath),
            _ => throw new ArgumentException("Incorrect LogType argument"),
        };
    }
    public static MultiFileLogger GetLogger(string filePath, int fileInstances)
    {
        return new MultiFileLogger(filePath, fileInstances);
    }
}

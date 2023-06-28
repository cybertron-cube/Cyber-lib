namespace Cybertron.CLogger;

public class SingleFileLog : BaseLogger, ILogger
{
    public override LogLevel DefaultLogLevel { get => _defaultLogLevel; set => _defaultLogLevel = value; }
    public string FilePath { get => _filePath; }
    private LogLevel _defaultLogLevel = LogLevel.Information;
    private readonly string _filePath;
    public SingleFileLog(string filePath)
    {
        _filePath = filePath + ".log";
    }
    private protected override void CustomLog(string message, LogLevel logLevel)
    {
        using (StreamWriter writer = new(_filePath, true))
        {
            writer.WriteLine($"{DateTime.Now} [{logLevel.ToString().ToUpper()}] : {message}");
            writer.Close();
        }
    }
}

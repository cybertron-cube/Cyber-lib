namespace Cybertron.CLogger;

public sealed class MultiFileLogger : BaseLogger, ILogger
{
    public override LogLevel DefaultLogLevel { get => _defaultLogLevel; set => _defaultLogLevel = value; }
    public string FilePath { get => _filePath; }
    public int FileInstances { get => _fileInstances; }
    private LogLevel _defaultLogLevel = LogLevel.Information;
    private string _filePath;
    private int _fileInstances;
    public MultiFileLogger(string filePath, int fileInstances)
    {
        _filePath = filePath;
        SetWriteFile(fileInstances);
    }
    public MultiFileLogger(string filePath)
    {
        _filePath = filePath;
        SetWriteFile(3);
    }
    private void SetWriteFile(int fileInstances)
    {
        _fileInstances = fileInstances;
        string fileName = Path.GetFileNameWithoutExtension(_filePath);
        string directory = Path.GetDirectoryName(_filePath)!;
        string path;
        for (int i = 1; i <= _fileInstances; i++)
        {
            path = $"{Path.Combine(directory, fileName)}{i}.log";
            if (File.Exists(path))
            {
                continue;
            }
            else
            {
                _filePath = path;
                return;
            }
        }
        _filePath = $"{Path.Combine(directory, fileName)}1.log";
        DateTime lastFileWriteTime = File.GetLastWriteTime($"{Path.Combine(directory, fileName)}1.log");
        DateTime thisFileWriteTime;
        for (int i = 2; i <= _fileInstances; i++)
        {
            path = $"{Path.Combine(directory, fileName)}{i}.log";
            thisFileWriteTime = File.GetLastWriteTime(path);
            if (DateTime.Compare(thisFileWriteTime, lastFileWriteTime) < 0)
            {
                lastFileWriteTime = thisFileWriteTime;
                _filePath = path;
            }
        }
        File.Delete(_filePath);
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

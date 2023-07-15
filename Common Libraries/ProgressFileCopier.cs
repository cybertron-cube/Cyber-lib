using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cybertron;

public delegate void ProgressChangeDelegate(double Percentage);
public delegate void Completedelegate();

public class ProgressFileCopier
{
    private string SourceFilePath = String.Empty;
    private string OutputFilePath = String.Empty;
    public event ProgressChangeDelegate OnProgressChanged;
    public event Completedelegate OnComplete;
    private long _CancelFlag;
    public bool CancelFlag
    {
        get => Interlocked.Read(ref _CancelFlag) == 1;
        set => Interlocked.Exchange(ref _CancelFlag, Convert.ToInt64(value));
    }

    public ProgressFileCopier()
    {
        OnProgressChanged += delegate { };
        OnComplete += delegate { };
    }
    public void CopyFile(string sourceFilePath, string outputFilePath) //change this to match the override below
    {
        SourceFilePath = sourceFilePath;
        OutputFilePath = outputFilePath;

        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

        using (FileStream source = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
        {
            long fileLength = source.Length;
            using (FileStream dest = new FileStream(OutputFilePath, FileMode.Create, FileAccess.Write))
            {
                long totalBytes = 0;
                int currentBlockSize = 0;

                while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytes += currentBlockSize;
                    double percentage = (double)totalBytes / fileLength;
                    dest.Write(buffer, 0, currentBlockSize);
                    OnProgressChanged(percentage);
                    if (CancelFlag == true)
                    {
                        return;
                    }
                }
            }
        }

        OnComplete();
    }
    private void CopyFile()
    {
        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

        using (FileStream source = new(SourceFilePath, FileMode.Open, FileAccess.Read))
        {
            long fileLength = source.Length;
            using (FileStream dest = new(OutputFilePath, FileMode.CreateNew, FileAccess.Write))
            {
                long totalBytes = 0;
                int currentBlockSize = 0;

                while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytes += currentBlockSize;
                    double percentage = (double)totalBytes / fileLength;
                    dest.Write(buffer, 0, currentBlockSize);
                    OnProgressChanged(percentage);
                    if (CancelFlag)
                    {
                        return;
                    }
                }
            }
        }
        OnComplete();
    }
    /*public async Task<string> CopyDirectory(string sourceDir, string outputDir, string ext)
    {
        DirectoryInfo dirInfo = new(sourceDir);
        var files = dirInfo.EnumerateFiles(ext);
        this.OnProgressChanged += ProgressFileCopier_OnProgressChanged;
        this.OnComplete += ProgressFileCopier_OnComplete;
        foreach (FileInfo file in files)
        {
            SourceFilePath = file.FullName;
            OutputFilePath = Path.Combine(outputDir, file.Name);
            File.Delete(OutputFilePath);
            CopyFile();
            if (CancelFlag)
            {
                File.Delete(OutputFilePath);
                return file.FullName;
            }
        }
        return "0";
    }*/
    public void Stop()
    {
        CancelFlag = true;
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cybertron;

public static class GenStatic
{
    public static void GetOSRespectiveExecutablePath(ref string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (path.EndsWith(".exe"))
            {
                return;
            }
            else
            {
                path += ".exe";
            }
        }
        else
        {
            if (path.EndsWith(".exe"))
            {
                path = path[..^4];
            }
        }
    }
    public static string GetOSRespectiveExecutablePath(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!path.EndsWith(".exe"))
            {
                path += ".exe";
            }
        }
        else
        {
            if (path.EndsWith(".exe"))
            {
                path = path[..^4];
            }
        }
        return path;
    }
    public static void ReplaceWinNewLine(ref string str)
    {
        str = str.Replace("\r\n", "\n");
    }
    public static Process GetOSRespectiveTerminalProcess(ProcessStartInfo processStartInfo)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            processStartInfo.FileName = "cmd.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            processStartInfo.FileName = "/bin/bash";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            processStartInfo.FileName = "/Applications/Utilities/Terminal.app";
        }

        var terminalProcess = new Process()
        {
            StartInfo = processStartInfo,
        };
        return terminalProcess;
    }
    public static Process GetOSRespectiveTerminalProcess()
    {
        var processStartInfo = new ProcessStartInfo();
        return GetOSRespectiveTerminalProcess(processStartInfo);
    }

    public static string GetFullPathFromRelative(string? relPath = null)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        return relPath == null ? basePath : Path.Combine(basePath, relPath);
    }
    public static string GetRelativePathFromFull(string baseDirPath, string fullPath)
    {
        if (Path.EndsInDirectorySeparator(baseDirPath))
        {
            return fullPath.Replace(baseDirPath, "");
        }
        else
        {
            return fullPath.Replace(baseDirPath + Path.DirectorySeparatorChar, "");
        }
    }
    public static void AppendFileName(ref string filePath, string appendage)
    {
        filePath = filePath.Replace(Path.GetFileName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}{appendage}{Path.GetExtension(filePath)}");
    }
    public static string AppendFileName(string filePath, string appendage)
    {
        return filePath.Replace(Path.GetFileName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}{appendage}{Path.GetExtension(filePath)}");
    }
    public static void IncrementFileName(ref string file)
    {
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        int digitStartIndex = GetStartIndexOfEndingDigits(fileNameWithoutExt);
        if (Int32.TryParse(fileNameWithoutExt[digitStartIndex..], out int result))
        {
            file = file.Replace(Path.GetFileName(file), $"{fileNameWithoutExt[..^(fileNameWithoutExt.Length - digitStartIndex)]}{++result}{Path.GetExtension(file)}");
        }
        else
        {
            file = file.Replace(Path.GetFileName(file), $"{Path.GetFileNameWithoutExtension(file)}{1}{Path.GetExtension(file)}");
        }
    }
    public static int GetStartIndexOfEndingDigits(string str)
    {
        for (int i = str.Length - 1; i >= 0; i--)
        {
            if (Char.IsDigit(str[i]))
            {
                continue;
            }
            else
            {
                return i + 1;
            }
        }
        throw new ArgumentException("The string does not contain any digits");
    }
    public static void IncrementFileNameUntilAvailable(ref string file)
    {
        while (File.Exists(file))
        {
            IncrementFileName(ref file);
        }
    }
}

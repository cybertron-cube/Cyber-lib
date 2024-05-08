using System.Diagnostics;

namespace Cybertron;

public static partial class GenStatic
{
    public static void ReplaceWinNewLine(ref string str)
    {
        str = str.Replace("\r\n", "\n");
    }

    public static string GetFullPathFromRelative(string? relPath = null)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        return relPath == null ? basePath : Path.Combine(basePath, relPath);
    }
    
    public static string GetRelativePathFromFull(string baseDirPath, string fullPath)
    {
        return Path.EndsInDirectorySeparator(baseDirPath)
            ? fullPath.Replace(baseDirPath, "")
            : fullPath.Replace(baseDirPath + Path.DirectorySeparatorChar, "");
    }
    
    public static void AppendFileName(ref string filePath, string appendage)
    {
        filePath = ReplaceLastOccurrence(filePath, Path.GetFileName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}{appendage}{Path.GetExtension(filePath)}");
    }
    
    public static string AppendFileName(string filePath, string appendage)
    {
        return ReplaceLastOccurrence(filePath, Path.GetFileName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}{appendage}{Path.GetExtension(filePath)}");
    }

    public static string ChangeExtension(string filePath, string newExtension)
    {
        if (!newExtension.StartsWith('.')) newExtension = '.' + newExtension;

        var oldExtension = Path.GetExtension(filePath);

        if (oldExtension == string.Empty) return filePath + newExtension;
        return ReplaceLastOccurrence(filePath, Path.GetExtension(filePath), newExtension);
    }

    public static string ReplaceLastOccurrence(string source, string oldValue, string newValue, StringComparison stringComparison = StringComparison.Ordinal)
    {
        var place = source.LastIndexOf(oldValue, stringComparison);
        return place == -1 ? source : source.Remove(place, oldValue.Length).Insert(place, newValue);
    }
    
    public static void IncrementFileName(ref string file)
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        var digitStartIndex = GetStartIndexOfEndingDigits(fileNameWithoutExt);
        
        file = file.Replace(Path.GetFileName(file),
            int.TryParse(fileNameWithoutExt[digitStartIndex..], out var result)
                ? $"{fileNameWithoutExt[..^(fileNameWithoutExt.Length - digitStartIndex)]}{++result}{Path.GetExtension(file)}"
                : $"{Path.GetFileNameWithoutExtension(file)}{1}{Path.GetExtension(file)}");
    }
    
    public static int GetStartIndexOfEndingDigits(string str)
    {
        for (int i = str.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(str[i]))
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
    
    /// <summary>
    /// Opens a weblink with the default browser assigned on the system
    /// </summary>
    /// <param name="url"></param>
    public static void OpenWebLink(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (OperatingSystem.IsWindows())
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else throw;
        }
    }
    
    public static bool ExecuteAndRetry(Action work, Action<Exception>? onCaughtException = null, int retryCount = 2, int delay = 100)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                work.Invoke();
                return true;
            }
            catch (Exception e)
            {
                onCaughtException?.Invoke(e);
                Thread.Sleep(delay);
            }
        }

        return false;
    }
    
    public static async Task<bool> ExecuteAndRetryAsync(Action work, Action<Exception>? onCaughtException = null, int retryCount = 2, int delay = 100)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                work.Invoke();
                return true;
            }
            catch (Exception e)
            {
                onCaughtException?.Invoke(e);
                await Task.Delay(delay);
            }
        }

        return false;
    }
}

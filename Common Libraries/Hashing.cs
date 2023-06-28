using System.Diagnostics;
using System.Security.Cryptography;

namespace Cybertron;

public class Hashing
{
    public Action<string>? OnNextFile;
    public Action<string>? OnCompleteFile;
    public enum HashingAlgorithmTypes
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512
    }
    public class CFileInfo
    {
        public FileInfo FileInfo;
        public string Hash;
        public CFileInfo(FileInfo fileInfo, string hash)
        {
            FileInfo = fileInfo;
            Hash = hash;
        }
    }
    public static string GetChecksum(string filePath, HashingAlgorithmTypes hashingAlgorithmType)
    {
        using (var hasher = HashAlgorithm.Create(hashingAlgorithmType.ToString()))
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = hasher!.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
    public static async Task<string> GetChecksumAsync(string filePath, HashingAlgorithmTypes hashingAlgorithmType, CancellationToken ct = default)
    {
        using (var hasher = HashAlgorithm.Create(hashingAlgorithmType.ToString()))
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = await hasher!.ComputeHashAsync(stream, ct);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
    public static List<CFileInfo> DirectoryHash(string inputDir, HashingAlgorithmTypes hashingAlgorithmType)
    {
        var dirInfo = new DirectoryInfo(inputDir);
        var files = dirInfo.EnumerateFiles("*");
        var fileList = new List<CFileInfo>();
        foreach (var file in files.OrderBy(x => x.Name))
        {
            fileList.Add(new CFileInfo(file, GetChecksum(file.FullName, hashingAlgorithmType)));
        }
        return fileList;
    }
    public async static Task<List<CFileInfo>> DirectoryHashAsync(string inputDir, HashingAlgorithmTypes hashingAlgorithmType)
    {
        return await Task.Run(() => DirectoryHash(inputDir, hashingAlgorithmType));
    }
    public static void DirectoryHash(string inputDir, string outputFilePath, HashingAlgorithmTypes hashingAlgorithmType)
    {
        var dirInfo = new DirectoryInfo(inputDir);
        var files = dirInfo.EnumerateFiles("*");
        using (var writer = new StreamWriter(outputFilePath))
        {
            foreach (var file in files)
            {
                writer.WriteLine($"{file.Name}      {GetChecksum(file.FullName, hashingAlgorithmType)}");
            }
        }
    }
    public async Task<string> DirectoryHashAsync(string inputDir, string outputFilePath, string searchPattern, HashingAlgorithmTypes hashingAlgorithmType, CancellationToken ct)
    {
        var dirInfo = new DirectoryInfo(inputDir);
        var files = dirInfo.EnumerateFiles(searchPattern).ToArray();
        string lastFileNameHashed = "0";
        if (File.Exists(outputFilePath))
        {
            GenStatic.IncrementFileNameUntilAvailable(ref outputFilePath);
        }
        using (var writer = new StreamWriter(outputFilePath) { AutoFlush = true })
        {
            foreach (var file in files)
            {
                OnNextFile?.Invoke(file.Name);
                string checksum;
                try
                {
                    checksum = await GetChecksumAsync(file.FullName, hashingAlgorithmType, ct);
                }
                catch (TaskCanceledException e)
                {
                    Debug.WriteLine(e.CancellationToken.IsCancellationRequested);
                    lastFileNameHashed = file.Name;
                    break;
                }
                if (ct.IsCancellationRequested)
                {
                    lastFileNameHashed = file.Name;
                    break;
                }
                await File.WriteAllTextAsync($"{file.FullName}.{hashingAlgorithmType.ToString().ToLower()}", $"{checksum.ToLower()} *{file.Name}", CancellationToken.None);
                await writer.WriteLineAsync($"{file.Name} {checksum.ToLower()}");
                OnCompleteFile?.Invoke(file.Name);
            }
        }
        return lastFileNameHashed;
    }
}
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Http.Headers;
using Cybertron.CUpdater.Github;
using System.Diagnostics;
using System.Formats.Tar;
using System.Runtime.Versioning;

namespace Cybertron.CUpdater;

public class Updater
{
    public const string UnixScript =
        """
        #!/bin/sh
        
        set -e
        
        SCRIPT_PATH="$0"
        UPDATER_PATH="$1"
        UPDATER_DIR=$( dirname "$UPDATER_PATH" )
        UPDATER_DIR_DIR=$( dirname "$UPDATER_DIR" )
        
        "$UPDATER_PATH" "$@"
        
        rm -rf "$UPDATER_DIR"
        mv "$UPDATER_DIR_DIR/updater_new" "$UPDATER_DIR_DIR/updater"
        
        rm -- "$SCRIPT_PATH"
        
        """;
    
    // scriptPath, updaterPath, ...
    public const string WindowsScript =
        """
        $ErrorActionPreference = "Stop"
        
        $SCRIPT_PATH = $MyInvocation.MyCommand.Path
        $UPDATER_PATH = $args[0]
        $UPDATER_DIR = Split-Path -Parent $UPDATER_PATH
        $UPDATER_DIR_DIR = Split-Path -Parent $UPDATER_DIR
        
        & $UPDATER_PATH $Args
        
        Remove-Item -Path $UPDATER_DIR -Recurse -Force
        Move-Item -Path "$UPDATER_DIR_DIR\updater_new" -Destination "$UPDATER_DIR_DIR\updater" -Force
        
        Remove-Item -Path $SCRIPT_PATH -Force
        
        """;
    
    public event Action<string>? OnNextFile;

    public record struct CheckUpdateResult(bool UpdateAvailable, string TagName = "", string Name = "",
        string? DownloadLink = "", string Body = "");
        
    //TODO have option for target_commitish (branch name)
    /// <summary>
    /// Retrieves the <see cref="GithubRelease"/> from github,
    /// gets a single asset with a name that contains each of the <paramref name="assetIdentifiers"/> from the <see cref="GithubRelease"/>,
    /// and returns a <see cref="CheckUpdateResult"/>
    /// </summary>
    /// <param name="appName">Github repo name/App name</param>
    /// <param name="assetIdentifiers">Strings to search for in each asset of the latest release</param>
    /// <param name="url">The github api url, example: "https://api.github.com/repos/cybertron-cube/cyber-lib"</param>
    /// <param name="currentVersion"></param>
    /// <param name="client"></param>
    /// <param name="includePreReleases"></param>
    /// <returns> <see cref="CheckUpdateResult"/> </returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException">If the asset page contains more than one asset that contains the assetIdentifier string</exception>
    /// <exception cref="NullReferenceException">Unable to properly create an IVersion instance from the received tag name</exception>
    public static async Task<CheckUpdateResult> GithubCheckForUpdatesAsync<TVersion>(string appName, IEnumerable<string> assetIdentifiers, string url, TVersion currentVersion, HttpClient client, bool includePreReleases = false) where TVersion : IVersion
    {
        var latestRelease = await GetLatestGithubRelease(appName, url, currentVersion, client, includePreReleases);
        var latestVersion = (TVersion?)Activator.CreateInstance(typeof(TVersion), latestRelease.tag_name);
        if (latestVersion is null) throw new NullReferenceException($"Could not properly create an instance of your type, {typeof(TVersion)}, of version from string {latestRelease.tag_name}");
            
        if (latestVersion.CompareTo(currentVersion) > 0)
        {
            return new CheckUpdateResult(true,
                latestRelease.tag_name,
                latestRelease.name,
                latestRelease.assets.Single(x => x.name.Contains(assetIdentifiers)).browser_download_url,
                latestRelease.body);
        }
            
        return new CheckUpdateResult(false);
    }

    public static async Task<GithubRelease> GetLatestGithubRelease<TVersion>(string appName, string url, TVersion currentVersion, HttpClient client, bool includePreReleases = false) where TVersion : IVersion
    {
        url += includePreReleases ? "/releases?per_page=1" : "/releases/latest";
        string responseJson;
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
        {
            requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(appName, currentVersion.ToString()));
            var response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            responseJson = await response.Content.ReadAsStringAsync();
        }
            
        if (includePreReleases)
        {
            var latestReleases = JsonConvert.DeserializeObject<GithubRelease[]>(responseJson);
                
            if (latestReleases is null)
                throw new NullReferenceException($"Could not deserialize json from github, {responseJson}");
                
            return latestReleases[0];
        }
        else
        {
            var latestRelease = JsonConvert.DeserializeObject<GithubRelease>(responseJson);
                
            if (latestRelease is null)
                throw new NullReferenceException($"Could not deserialize json from github, {responseJson}");
                
            return latestRelease;
        }
    }

    public static void StartUpdater(string updaterPath, string downloadLink, string extractDestination, string wildCardPreserve, IEnumerable<string> preservables)
    {
        string? appToLaunch;
        string procName;
        using (var thisProcess = Process.GetCurrentProcess())
        {
            procName = thisProcess.ProcessName;
            appToLaunch = thisProcess.MainModule?.FileName;
        }
        
        if (appToLaunch is null)
            throw new NullReferenceException("Could not obtain filename from process main module");
        
        
        var scriptPath = Path.GetTempFileName();
        
        ProcessStartInfo processStartInfo;
        if (OperatingSystem.IsWindows())
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                Verb = "runas"
            };
            
            File.WriteAllText(scriptPath, WindowsScript);
        }
        else
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh"
            };
            
            File.WriteAllText(scriptPath, UnixScript);
        }
        processStartInfo.ArgumentList.Add(scriptPath);
        
        var args = new UpdaterArgs(updaterPath, procName, downloadLink, extractDestination, appToLaunch, wildCardPreserve,
            preservables.ToListWithCast());
        
        ArgsHelper.AddToProcessStartInfo(processStartInfo, args);
        
        Process.Start(processStartInfo);
    }
    
    public static async Task DownloadUpdatesProgressAsync(string downloadLink, string destDir, HttpClient client, IProgress<double> progress)
    {
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadLink))
        {
            var downloadWithProgress = new HttpClientProgress(downloadLink, destDir, client);
            downloadWithProgress.OnProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                progress.Report((double)progressPercentage);
            };
            await downloadWithProgress.StartDownload(requestMessage);
        }
    }
    
    /// <summary>
    /// Extracts a compressed file to a directory with progress updates
    /// </summary>
    /// <param name="pathExtract">Path to the file to extract</param>
    /// <param name="pathDestination">Path to the destination directory to extract contents</param>
    /// <param name="ignorables">Files to ignore by name</param>
    /// <param name="progress">Use a thread safe progress type for accuracy</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="PlatformNotSupportedException">GZip is not supported on windows</exception>
    /// <exception cref="NotImplementedException"></exception>
    public async Task ExtractToDirectoryProgressAsync(string pathExtract, string pathDestination, IEnumerable<string> ignorables, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        if (FileSignatures.IsZip(pathExtract))
            await ExtractZipFileProgressAsync(pathExtract, pathDestination, ignorables, progress, cancellationToken);
        else if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The detected file type does not support extraction on windows");
        else if (FileSignatures.IsGZip(pathExtract))
            await ExtractGZipFileProgressAsync(pathExtract, pathDestination, ignorables, progress, cancellationToken);
        else throw new NotImplementedException("Only Zip and GZip file extraction are currently implemented");
    }
    
    public async Task ExtractZipFileProgressAsync(string pathZip, string pathDestination, IEnumerable<string> ignorables, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        using var archive = ZipFile.OpenRead(pathZip);
        long currentProgression = 0;
        var firstEntryCount = 0;
        var entries = archive.Entries.Where(x => ignorables.All(y => x.FullName.Replace('\\', '/') != y.Replace('\\', '/'))).ToList();
        var totalLength = entries.Sum(entry => (double)entry.Length);

        //check if there is only one root folder, if so then skip making that folder
        var rootDirEntries = entries.Where(x => (x.FullName.EndsWith('/') || x.FullName.EndsWith('\\')) && x.FullName.Count(c => c is '/' or '\\') == 1);
        ZipArchiveEntry? rootDirEntry;
        if (rootDirEntries.Count() == 1)
        {
            rootDirEntry = rootDirEntries.Single();
            
            // TODO TEST THIS CHANGE
            if (entries.All(x => x.FullName.StartsWith(rootDirEntry.FullName)))
            {
                firstEntryCount = rootDirEntry.FullName.Length;
                entries = archive.Entries.Where(x => ignorables.All(y =>
                    x.FullName.Replace('\\', '/').Replace(rootDirEntry.FullName, "") != y.Replace('\\', '/'))).ToList();
            }
        }
        else rootDirEntry = null;

        foreach (var entry in entries.Where(x => x != rootDirEntry))
        {
            // Check if entry is a folder
            var filePath = Path.Combine(pathDestination, entry.FullName[firstEntryCount..]);
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(filePath);
                continue;
            }
                
            OnNextFile?.Invoke(filePath);
                
            // Create folder anyway since a folder may not have an entry
            var baseDir = Path.GetDirectoryName(filePath);
            if (baseDir is not null)
                Directory.CreateDirectory(baseDir);
            
            // WARNING - UPDATER SPECIFIC
            // This locks in the "updater" folder requirement
            if (Path.GetFileName(baseDir) == "updater")
            {
                filePath = Path.Combine(Path.GetDirectoryName(baseDir), "updater_new", Path.GetFileName(filePath));
            }

            await using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using (var entryStream = entry.Open())
                {
                    var relativeProgress = new Progress<long>(fileProgressBytes => progress.Report((fileProgressBytes + currentProgression) / totalLength));
                    await entryStream.CopyToAsync(file, 81920, relativeProgress, cancellationToken);
                }
            }
            currentProgression += entry.Length;
        }
    }
    
    [UnsupportedOSPlatform("windows")]
    public async Task ExtractGZipFileProgressAsync(string pathZip, string pathDestination, IEnumerable<string> ignorables, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await using var fs = new FileStream(pathZip, FileMode.Open, FileAccess.Read);
        await using var gzip = new GZipStream(fs, CompressionMode.Decompress);
        await using var reader = new TarReader(gzip);
        
        var entries = new List<TarEntry>();
        var rootDirEntries = new List<TarEntry>();
        
        while (await reader.GetNextEntryAsync(true, cancellationToken) is { } entry)
        {
            if (OperatingSystem.IsMacOS() && (Path.GetFileName(entry.Name).StartsWith("._") || Path.GetFileName(entry.Name).Equals(".ds_store", StringComparison.CurrentCultureIgnoreCase)))
                continue;
            
            if (entry.Name.EndsWith('/') && entry.Name.Count(x => x == '/') == 1)
            {
                rootDirEntries.Add(entry);
            }
            
            entries.Add(entry);
        }

        var rootDirEntryNameLength = 0;
        if (rootDirEntries.Count == 1)
        {
            var rootDirEntry = rootDirEntries.Single();
            
            // Ensure all entries use root directory entry - reason: there could be root file entries
            if (rootDirEntries.All(x => x.Name.StartsWith(rootDirEntry.Name)))
            {
                rootDirEntryNameLength = rootDirEntry.Name.Length;
                entries.Remove(rootDirEntry);
            }
        }

        long currentProgression = 0;
        var totalLength = entries.Sum(entry => (double)entry.Length);
        // ReSharper disable once AccessToModifiedClosure
        var relativeProgress = new ThreadSafeProgress<long>(fileProgressBytes => 
            progress.Report((fileProgressBytes + currentProgression) / totalLength));
        
        // Descending so directories come first - TODO will have to change if implementing new entry types
        foreach (var entry in entries.OrderByDescending(x => x.EntryType)
                     .ThenBy(x => x.Name.Count(c => c == '/')))
        {
            var path = Path.Combine(pathDestination,
                rootDirEntryNameLength == 0 ? entry.Name : entry.Name[rootDirEntryNameLength..]);
            
            if (entry.EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(path);
                File.SetUnixFileMode(path, entry.Mode);
            }
            else if (entry.EntryType == TarEntryType.RegularFile)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                if (ignorables.Any(x =>
                        string.Equals(Path.GetFileName(entry.Name), x, StringComparison.CurrentCultureIgnoreCase)))
                    continue;
                
                if (entry.DataStream is null)
                    throw new NullReferenceException("The tar entry data stream is null");
                
                OnNextFile?.Invoke(path);

                var baseDir = Path.GetDirectoryName(path);
                if (baseDir is not null)
                    Directory.CreateDirectory(baseDir);
                
                // WARNING - UPDATER SPECIFIC
                // This locks in the "updater" folder requirement
                if (Path.GetFileName(baseDir) == "updater")
                {
                    path = Path.Combine(Path.GetDirectoryName(baseDir), "updater_new", Path.GetFileName(path));
                }
                
                await using (var newFile = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    File.SetUnixFileMode(path, entry.Mode);
                    await entry.DataStream.CopyToAsync(newFile, 81920, relativeProgress, cancellationToken);
                }
                
                Interlocked.Add(ref currentProgression, entry.Length);
            }
            else
            {
                throw new NotImplementedException("Regular file and directory are the only entry types implemented");
            }
        }
    }
}
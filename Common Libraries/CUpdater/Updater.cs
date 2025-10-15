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
    private const string UnixScriptUrl =
        "https://raw.githubusercontent.com/cybertron-cube/Cyber-lib/refs/heads/main/UpdaterScripts/UnixScript.sh";
    
    private const string WindowsScriptUrl =
        "https://raw.githubusercontent.com/cybertron-cube/Cyber-lib/refs/heads/main/UpdaterScripts/WindowsScript.ps1";
    
    public event Action<string>? OnNextFile;

    public record struct CheckUpdateResult(bool UpdateAvailable, string TagName = "", string Name = "",
        string? DownloadLink = "", string Body = "");
        
    //TODO have option for target_commitish (branch name)
    /// <summary>
    /// Retrieves the latest <see cref="GithubRelease"/> from the <paramref name="apiUrl"/>,
    /// gets a single <see cref="GithubAsset"/> from the <see cref="GithubRelease"/> determined by <paramref name="assetResolver"/>,
    /// and returns a <see cref="CheckUpdateResult"/>.
    /// </summary>
    /// <param name="appName">Github repo name/App name</param>
    /// <param name="apiUrl">The github api url, example: https://api.github.com/repos/cybertron-cube/cyber-lib</param>
    /// <param name="currentVersion">The currently installed version of the requested application</param>
    /// <param name="assetResolver">A func that returns true for only a single <see cref="GithubAsset"/> within a <see cref="GithubRelease"/></param>
    /// <param name="includePreReleases">Whether to include <see cref="GithubRelease"/>s that are marked as pre-release</param>
    /// <param name="client">An optional <see cref="HttpClient"/> to use, if not specified, a new one will be created</param>
    /// <returns>A <see cref="CheckUpdateResult"/> that contains information specific to the resolved <see cref="GithubRelease"/> and <see cref="GithubAsset"/></returns>
    /// <exception cref="HttpRequestException">An error occured attempting to send an http request to the github <paramref name="apiUrl"/></exception>
    /// <exception cref="InvalidOperationException">If the <see cref="GithubRelease"/> contains more than one or no <see cref="GithubAsset"/> resolved using <paramref name="assetResolver"/></exception>
    /// <exception cref="NullReferenceException">Unable to properly create an IVersion instance from the received tag name. Make sure If you are using your own version specification inherited from
    /// <see cref="IVersion"/>, that it has a constructor with a single string argument that would represent a version under your scheme</exception>
    public static async Task<CheckUpdateResult> GithubCheckForUpdatesAsync<TVersion>(string appName, string apiUrl, TVersion currentVersion, Func<GithubAsset, bool> assetResolver, bool includePreReleases = false, HttpClient? client = null) where TVersion : IVersion
    {
        client ??= new HttpClient();
        
        var latestRelease = await GetLatestGithubRelease(appName, apiUrl, currentVersion, client, includePreReleases);
        var latestVersion = (TVersion?)Activator.CreateInstance(typeof(TVersion), latestRelease.tag_name);
        if (latestVersion is null) throw new NullReferenceException($"Could not properly create an instance of your type, {typeof(TVersion)}, of version from string {latestRelease.tag_name}");
            
        if (latestVersion.CompareTo(currentVersion) > 0)
        {
            return new CheckUpdateResult(true,
                latestRelease.tag_name,
                latestRelease.name,
                latestRelease.assets.Single(assetResolver).browser_download_url,
                latestRelease.body);
        }
            
        return new CheckUpdateResult(false);
    }

    public static async Task<GithubRelease> GetLatestGithubRelease<TVersion>(string appName, string apiUrl, TVersion currentVersion, HttpClient client, bool includePreReleases = false) where TVersion : IVersion
    {
        apiUrl += includePreReleases ? "/releases?per_page=1&page=1" : "/releases/latest";
        string responseJson;
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl))
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
    
    /// <summary>
    /// Starts the updater executable with the needed arguments
    /// </summary>
    /// <param name="updaterPath">Path to the updater executable</param>
    /// <param name="downloadLink">Download link that can be obtained from <see cref="GithubCheckForUpdatesAsync{TVersion}"/></param>
    /// <param name="extractDestination">Where to extract the archive downloaded from the link</param>
    /// <param name="wildCardPreserve">Range of file names to preserve when extracting (separate multiple entries with ';')</param>
    /// <param name="preservables">Specific file names to preserve when extracting</param>
    /// <param name="updaterScriptLogFilePath">Log path for updater script to output to</param>
    /// <param name="httpClient"></param>
    /// <returns>Path to a temporary updater script</returns>
    /// <exception cref="NullReferenceException"></exception>
    public static async Task<string> StartUpdater(string updaterPath, string downloadLink, string extractDestination, string wildCardPreserve, IEnumerable<string> preservables, string? updaterScriptLogFilePath = null, HttpClient? httpClient = null)
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
        
        var scriptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        httpClient ??= new HttpClient();
        
        ProcessStartInfo processStartInfo;
        if (OperatingSystem.IsWindows())
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                CreateNoWindow = true
            };
            
            processStartInfo.Arguments += "-NoProfile -ExecutionPolicy Bypass -File ";
            
            var windowsScript = await httpClient.GetStringAsync(WindowsScriptUrl);
            windowsScript = windowsScript.Replace("|PathToScriptLogFile|", updaterScriptLogFilePath);
            
            // Make windows happy ;)
            // Otherwise there will be a prompt for associating a file extension
            scriptPath = Path.ChangeExtension(scriptPath, "ps1");
            
            await File.WriteAllTextAsync(scriptPath, windowsScript);
        }
        else
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh"
            };
            
            var unixScript = await httpClient.GetStringAsync(UnixScriptUrl);
            unixScript = unixScript.Replace("|PathToScriptLogFile|", updaterScriptLogFilePath);
            
            await File.WriteAllTextAsync(scriptPath, unixScript);
        }
        
        //-noexit
        processStartInfo.Arguments += $"\"{scriptPath}\"";
        
        var args = new UpdaterArgs(updaterPath, procName, downloadLink, extractDestination, appToLaunch, wildCardPreserve,
            preservables.ToListWithCast());
        
        ArgsHelper.AddToProcessStartInfo(processStartInfo, args);
        
        Process.Start(processStartInfo);

        return scriptPath;
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
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
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

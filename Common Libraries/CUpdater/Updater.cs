using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Http.Headers;
using Cybertron.CUpdater.Github;
using System.Diagnostics;

namespace Cybertron.CUpdater;

public class Updater
{
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
        string appToLaunch;
        using (var thisProcess = Process.GetCurrentProcess())
        {
            appToLaunch = thisProcess.MainModule.FileName;
        }

        ProcessStartInfo processStartInfo;
        if (OperatingSystem.IsWindows())
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                UseShellExecute = true,
                Verb = "runas"
            };
        }
        else
        {
            processStartInfo = new ProcessStartInfo
            {
                FileName = updaterPath
            };
        }

        processStartInfo.ArgumentList.Add(downloadLink);
        processStartInfo.ArgumentList.Add(extractDestination);
        processStartInfo.ArgumentList.Add(appToLaunch);
        processStartInfo.ArgumentList.Add(wildCardPreserve);
        foreach (var preservable in preservables)
        {
            processStartInfo.ArgumentList.Add(preservable);
        }

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
        
    public async Task ExtractToDirectoryProgressAsync(string pathZip, string pathDestination, IEnumerable<string> ignorables, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        using (ZipArchive archive = ZipFile.OpenRead(pathZip))
        {
            long currentProgression = 0;
            int firstEntryCount = 0;
            var entries = archive.Entries.Where(x => !ignorables.Any(y => x.FullName.Replace('\\', '/') == y.Replace('\\', '/')));
            long totalLength = entries.Sum(entry => entry.Length);

            //check if there is only one root folder, if so then skip making that folder
            var rootDirEntries = entries.Where(x => (x.FullName.EndsWith('/') || x.FullName.EndsWith('\\')) && (x.FullName.Count(x => x == '/' || x == '\\') == 1));
            ZipArchiveEntry? rootDirEntry;
            if (rootDirEntries.Count() == 1)
            {
                rootDirEntry = rootDirEntries.Single();
                firstEntryCount = rootDirEntry.FullName.Length;
                entries = archive.Entries.Where(x => !ignorables.Any(y => x.FullName.Replace('\\', '/').Replace(rootDirEntry.FullName, "") == y.Replace('\\', '/')));
            }
            else rootDirEntry = null;

            foreach (var entry in entries.Where(x => x != rootDirEntry))
            {
                // Check if entry is a folder
                string filePath = Path.Combine(pathDestination, entry.FullName[firstEntryCount..]);
                if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                {
                    Directory.CreateDirectory(filePath);
                    continue;
                }
                OnNextFile?.Invoke(filePath);
                // Create folder anyway since a folder may not have an entry
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var entryStream = entry.Open())
                    {
                        var relativeProgress = new Progress<long>(fileProgressBytes => progress.Report((double)(fileProgressBytes + currentProgression) / totalLength));
                        await entryStream.CopyToAsync(file, 81920, relativeProgress, cancellationToken);
                    }
                }
                currentProgression += entry.Length;
            }
        }
    }
}
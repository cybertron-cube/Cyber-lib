using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Http.Headers;
using Cybertron.CUpdater.Github;
using System.Globalization;
using System;

namespace Cybertron.CUpdater
{
    public class Updater
    {
        public event Action<string>? OnNextFile;
        public struct CheckUpdateResult
        {
            public bool UpdateAvailable;
            public string Version;
            public string DownloadLink;
            public CheckUpdateResult(bool updateAvailable, string version, string downloadLink)
            {
                UpdateAvailable = updateAvailable;
                Version = version;
                DownloadLink = downloadLink;
            }
            public CheckUpdateResult(bool updateAvailable)
            {
                UpdateAvailable = updateAvailable;
                Version = String.Empty;
                DownloadLink = String.Empty;
            }
        }
        //should have a way to supply your own comparison method use func<string, string, bool> as param
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="assetIdentifier"></param>
        /// <param name="url"></param>
        /// <param name="currentVersion"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="InvalidOperationException">if the asset page contains more than one asset that contains the assetIdentifier string</exception>
        /// <exception cref="NullReferenceException"></exception>
        public static async Task<CheckUpdateResult> CheckForUpdatesGitAsync(string appName, string assetIdentifier, string url, string currentVersion, HttpClient client)
        {
            string responseJson;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(appName, currentVersion));
                var response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();
                responseJson = await response.Content.ReadAsStringAsync();
            }
            var latest = JsonConvert.DeserializeObject<GithubLatestRelease>(responseJson);
            Version latestVersion = new(latest.tag_name);
            if (latestVersion.CompareTo(new Version(currentVersion)) > 0)
            {
                return new CheckUpdateResult(true, latest.tag_name, latest.assets.Where(x => x.name.Contains(assetIdentifier)).Single().browser_download_url);
            }
            else return new CheckUpdateResult(false);
        }
        public static async Task<CheckUpdateResult> CheckForUpdatesLatestByPublishGitAsync(string appName, string assetIdentifier, string url, string currentVersion, HttpClient client)
        {
            string responseJson;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(appName, currentVersion));
                var response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();
                responseJson = await response.Content.ReadAsStringAsync();
            }
            var latest = JsonConvert.DeserializeObject<GithubLatestRelease[]>(responseJson);
            var latestRelease = latest.MaxBy(x => DateTime.ParseExact(x.published_at, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            Version latestVersion = new(latestRelease.tag_name);
            if (latestVersion.CompareTo(new Version(currentVersion)) > 0)
            {
                return new CheckUpdateResult(true, latestRelease.tag_name, latestRelease.assets.Where(x => x.name.Contains(assetIdentifier)).Single().browser_download_url);
            }
            else return new CheckUpdateResult(false);
        }
        public static async Task<CheckUpdateResult> CheckForUpdatesPreIncludeGitAsync(string appName, string assetIdentifier, string url, string currentVersion, HttpClient client)
        {
            string responseJson;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(appName, currentVersion));
                var response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();
                responseJson = await response.Content.ReadAsStringAsync();
            }
            var latest = JsonConvert.DeserializeObject<GithubLatestRelease[]>(responseJson);
            var latestRelease = latest[0];
            Version latestVersion = new(latestRelease.tag_name);
            if (latestVersion.CompareTo(new Version(currentVersion)) > 0)
            {
                return new CheckUpdateResult(true, latestRelease.tag_name, latestRelease.assets.Where(x => x.name.Contains(assetIdentifier)).Single().browser_download_url);
            }
            else return new CheckUpdateResult(false);
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
        public async Task ExtractToDirectoryProgressAsync(string pathZip, string pathDestination, string[] ignorables, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            using (ZipArchive archive = ZipFile.OpenRead(pathZip))
            {
                long currentProgression = 0;
                int firstEntryCount = 0;
                var entries = archive.Entries.Where(x => !ignorables.Any(y => x.Name == y));
                long totalLength = entries.Sum(entry => entry.Length);

                //check if there is only one root folder, if so then skip making that folder
                var rootDirEntries = entries.Where(x => (x.FullName.EndsWith('/') || x.FullName.EndsWith('\\')) && (x.FullName.Count(x => x == '/' || x == '\\') == 1));
                ZipArchiveEntry? rootDirEntry;
                if (rootDirEntries.Count() == 1)
                {
                    rootDirEntry = rootDirEntries.Single();
                    firstEntryCount = rootDirEntry.FullName.Length;
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
}

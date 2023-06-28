namespace Cybertron.CUpdater;

public class HttpClientProgress
{
    private readonly string _downloadUrl;
    private readonly string _destinationFilePath;
    private readonly HttpClient _httpClient;
    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);
    public event ProgressChangedHandler? OnProgressChanged;
    public HttpClientProgress(string downloadUrl, string destinationFilePath, HttpClient httpClient)
    {
        _downloadUrl = downloadUrl;
        _destinationFilePath = destinationFilePath;
        _httpClient = httpClient;
    }
    public async Task StartDownload()
    {
        using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            await DownloadFileFromHttpResponseMessage(response);
    }
    public async Task StartDownload(HttpRequestMessage requestMessage)
    {
        using (var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
            await DownloadFileFromHttpResponseMessage(response);
    }
    private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;
        using (var contentStream = await response.Content.ReadAsStreamAsync())
            await ProcessContentStream(totalBytes, contentStream);
    }
    private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
    {
        var totalBytesRead = 0L;
        var readCount = 0L;
        var buffer = new byte[8192];
        var isMoreToRead = true;
        using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
        {
            do
            {
                var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    continue;
                }
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;
                readCount += 1;
                if (readCount % 100 == 0)
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }
            while (isMoreToRead);
        }
    }
    private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
    {
        if (OnProgressChanged == null)
            return;
        double? progressPercentage = null;
        if (totalDownloadSize.HasValue)
            progressPercentage = (double)totalBytesRead / totalDownloadSize.Value;
        OnProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
    }
}

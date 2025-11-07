namespace Cybertron.CUpdater;


public record UpdaterArgs(string UpdaterPath, string ProcName, string DownloadLink, string ExtractDestination,
    string AppToLaunch, IEnumerable<string> WildCardPreserve, IEnumerable<string> Preservables);

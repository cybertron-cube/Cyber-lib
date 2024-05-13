namespace Cybertron.CUpdater;

// WARNING - Limited to one IEnumerable<string> at the the end, all before must be of type string
public record UpdaterArgs(string UpdaterPath, string ProcName, string DownloadLink, string ExtractDestination,
    string AppToLaunch, string WildCardPreserve, List<string> Preservables);

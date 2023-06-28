namespace Cybertron.CAppSettings;

internal class Settings : PropertyReflection
{
    public string FFmpegPath { get; set; } = "";
    public string FrameCountMethod { get; set; } = "GetFrameCountApproximate";
    public bool AutoOverwriteCheck { get; set; }
    public bool CopySourceCheck { get; set; }
}

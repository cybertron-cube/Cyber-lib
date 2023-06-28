namespace Cybertron.CAppSettings;

public class Profile : PropertyReflection //maybe add position element
{
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string OutputExtension { get; set; } = "";
}

using Avalonia.Controls;
using System;

namespace AvaloniaMessageBox.Base;

public class BoxParamsBase
{
    public string Title { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public WindowStartupLocation StartupLocation { get; set; } = WindowStartupLocation.CenterScreen;
}

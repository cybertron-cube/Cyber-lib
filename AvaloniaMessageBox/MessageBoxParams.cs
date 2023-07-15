using AvaloniaMessageBox.Base;

namespace AvaloniaMessageBox;

public class MessageBoxParams : BoxParamsBase
{
    public MessageBoxButtons Buttons { get; set; } = MessageBoxButtons.Ok;
    public MessageBoxResult DefaultResult { get; set; } = MessageBoxResult.None;
}

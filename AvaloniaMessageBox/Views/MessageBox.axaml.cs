using Avalonia.Controls;
using AvaloniaMessageBox.Base;

namespace AvaloniaMessageBox;

public partial class MessageBox : Window, IWindowGetResult<MessageBoxResult>
{
    private MessageBoxResult FinalResult;
    public MessageBox()
    {
        InitializeComponent();
    }
    public MessageBoxResult GetResult() => FinalResult;
    public static IMessageBoxWindow<MessageBoxResult> GetMessageBox(MessageBoxParams @params)
    {
        var msgbox = new MessageBox()
        {
            FinalResult = @params.DefaultResult,
            Title = @params.Title,
            WindowStartupLocation = @params.StartupLocation,
        };
        msgbox.HeaderBlock.Text = @params.Header;
        msgbox.TextBlock.Text = @params.Message;
        var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

        void AddButton(string caption, MessageBoxResult result)
        {
            var btn = new Button { Content = caption };
            btn.Click += (_, __) =>
            {
                msgbox.FinalResult = result;
                msgbox.Close();
            };
            buttonPanel.Children.Add(btn);
        }

        if (@params.Buttons == MessageBoxButtons.Ok || @params.Buttons == MessageBoxButtons.OkCancel)
            AddButton("Ok", MessageBoxResult.Ok);
        if (@params.Buttons == MessageBoxButtons.YesNo || @params.Buttons == MessageBoxButtons.YesNoCancel)
        {
            AddButton("Yes", MessageBoxResult.Yes);
            AddButton("No", MessageBoxResult.No);
        }

        if (@params.Buttons == MessageBoxButtons.OkCancel || @params.Buttons == MessageBoxButtons.YesNoCancel)
            AddButton("Cancel", MessageBoxResult.Cancel);

        return new MsgBoxWindowBase<MessageBox, MessageBoxResult>(msgbox);
    }
}

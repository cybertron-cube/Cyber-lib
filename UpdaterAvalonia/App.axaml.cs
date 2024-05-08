using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cybertron;
using System;
using Cybertron.CUpdater;

namespace UpdaterAvalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
#if !DEBUG
            var mainWindow = desktop.MainWindow as MainWindow;
            desktop.Startup += (sender, args) =>
            {
                if (args.Args.Length == 0)
                {
                    Environment.Exit(1);
                }
                
                mainWindow!.UpdaterArgs = ArgsHelper.ArrayToArgs(args.Args);
            };
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }
}
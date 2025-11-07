using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;
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
            
            var mainWindow = desktop.MainWindow as MainWindow;
            desktop.Startup += (sender, args) =>
            {
                if (args.Args.Length == 0)
                {
                    Environment.Exit(1);
                }

                var json = File.ReadAllText(args.Args[0]);

                var updaterArgs = JsonSerializer.Deserialize(json, UpdaterArgsJsonContext.Default.UpdaterArgs);

                if (updaterArgs is null)
                    throw new Exception($"Could not deserialize json: {json}");
                
                mainWindow!.UpdaterArgs = updaterArgs;
            };
            
        }

        base.OnFrameworkInitializationCompleted();
    }
}

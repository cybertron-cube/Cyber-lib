using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UpdaterAvalonia
{
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
                MainWindow mainWindow = desktop.MainWindow as MainWindow;
                var desktopLifetime = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                desktopLifetime!.Startup += (sender, args) =>
                {
                    if (args.Args.Length == 0)
                    {
                        Environment.Exit(0);
                    }

                    string[] ignore = new string[1];
                    using (var thisProcess = Process.GetCurrentProcess())
                    {
                        ignore[0] = Path.GetFileName(thisProcess.MainModule.FileName);
                    }

                    if (args.Args.Length >= 3)
                    {
                        mainWindow.DownloadLink = args.Args[0];
                        mainWindow.ExtractDestPath = args.Args[1];
                        mainWindow.AppToLaunchPath = args.Args[2];
                    }
                    if (args.Args.Length > 3)
                    {
                        mainWindow.Ignorables = ignore.Concat(args.Args[3..]).ToArray();
                    }
                    else
                    {

                        mainWindow.Ignorables = ignore;
                    }
                };
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
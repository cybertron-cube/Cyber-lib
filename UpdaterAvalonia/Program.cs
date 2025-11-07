using Avalonia;
using System;
using System.IO;

namespace UpdaterAvalonia;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnExit;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
            
        return;

        void OnExit(object sender, UnhandledExceptionEventArgs e)
        {
            var path = Path.Combine(Path.GetTempPath(), "CybertronUpdaterError.log");
            File.WriteAllText(path, e.ExceptionObject.ToString());
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

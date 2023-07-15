using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cybertron;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

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
                        Environment.Exit(1);
                    }

                    var baseDir = args.Args[1];
#if RELEASEPORTABLE
                    //this build configuration expects the updater to be in its own folder within the app directory
                    List<string> ignoreList = new(53);
                    string dir;
                    using (var thisProcess = Process.GetCurrentProcess())
                    {
                        dir = Path.GetDirectoryName(thisProcess.MainModule.FileName);
                    }
                    var dirInfo = new DirectoryInfo(dir);
                    foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        ignoreList.Add(GenStatic.GetRelativePathFromFull(baseDir, file.FullName));
                    }
                    string[] ignore = ignoreList.ToArray();
#elif RELEASENATIVEEXTRACT
                    string[] ignore = new string[1];
                    using (var thisProcess = Process.GetCurrentProcess())
                    {
                        ignore[0] = GenStatic.GetRelativePathFromFull(baseDir, thisProcess.MainModule.FileName);
                    }
#else
                    //else self contained and single file with native AOT
                    string updaterExePath;
                    using (var thisProcess = Process.GetCurrentProcess())
                    {
                        updaterExePath = thisProcess.MainModule.FileName;
                    }
                    var updaterDir = Path.GetDirectoryName(updaterExePath);

                    //Could have conditional build configurations for below
                    string[] ignore;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        ignore = new string[4];
                        ignore[0] = GenStatic.GetRelativePathFromFull(baseDir, updaterExePath);
                        ignore[1] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "av_libglesv2.dll"));
                        ignore[2] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libHarfBuzzSharp.dll"));
                        ignore[3] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libSkiaSharp.dll"));
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        ignore = new string[3];
                        ignore[0] = GenStatic.GetRelativePathFromFull(baseDir, updaterExePath);
                        ignore[1] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libHarfBuzzSharp.so"));
                        ignore[2] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libSkiaSharp.so"));
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        ignore = new string[4];
                        ignore[0] = GenStatic.GetRelativePathFromFull(baseDir, updaterExePath);
                        ignore[1] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libAvaloniaNative.dylib"));
                        ignore[2] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libHarfBuzzSharp.dylib"));
                        ignore[3] = GenStatic.GetRelativePathFromFull(baseDir, Path.Combine(updaterDir, "libSkiaSharp.dylib"));
                    }
                    else throw new PlatformNotSupportedException();
#endif


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
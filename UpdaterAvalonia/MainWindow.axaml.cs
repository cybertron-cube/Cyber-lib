using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using Cybertron.CUpdater;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;
using Cybertron;

namespace UpdaterAvalonia;

public partial class MainWindow : Window
{
    public readonly HttpClient HttpClient = new();
    public readonly string DownloadPath = Path.GetTempFileName();
    public string DownloadLink;
    public string ExtractDestPath;
    public string AppToLaunchPath;
    public string[] WildcardPreserves;
    public List<string> Preservables;
    public MainWindow()
    {
        InitializeComponent();
#if !DEBUG
        Loaded += MainWindow_Loaded;
#else
        var testButton = new Button();
        UIPanel.Children.Add(testButton);
        testButton.Content = "Test";
        testButton.HorizontalAlignment = HorizontalAlignment.Right;
        testButton.Click += TestButton_Click!;

        UIProgress.Value = 0.5;
#endif
    }
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dirInfo = new DirectoryInfo(ExtractDestPath);
            if (WildcardPreserves[0] != string.Empty)
            {
                foreach (var searchPattern in WildcardPreserves)
                {
                    var addFiles = dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);
                    foreach (var addFile in addFiles)
                    {
                        Preservables.Add(GenStatic.GetRelativePathFromFull(ExtractDestPath, addFile.FullName));
                    }
                }
            }
            
            var debug = DownloadLink + Environment.NewLine + DownloadPath + Environment.NewLine
                        + ExtractDestPath + Environment.NewLine + AppToLaunchPath + Environment.NewLine
                        + "--IGNORABLES--" + Environment.NewLine + string.Join(Environment.NewLine, Preservables);
            
            await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"), debug);
            
            //Download the update (zip file)
            UILabel.Text = "Downloading update...";
            await Updater.DownloadUpdatesProgressAsync(DownloadLink, DownloadPath, HttpClient,
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));
            
            //Remove files that aren't in preservables
            UILabel.Text = "Removing files...";
            var intersect = dirInfo.EnumerateFiles().ExceptBy(Preservables, x => x.Name);
            var total = intersect.Count();
            var current = 0;
            foreach (var file in intersect)
            {
                file.Delete();
                current++;
                UIProgress.Value = (double)current / total;
            }

            //Extract zip file contents to destination
            var updater = new Updater();
            updater.OnNextFile += Updater_OnNextFile;
            await updater.ExtractToDirectoryProgressAsync(
                DownloadPath,
                ExtractDestPath,
                Preservables,
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));

            //Remove zip file, start process, then exit
            UILabel.Text = "Deleting temporary install files...";
            UIProgress.IsVisible = false;
            File.Delete(DownloadPath);
            Process.Start(AppToLaunchPath);
            Close(); 
        }
        catch (Exception ex)
        {
            File.Delete(DownloadPath);
            await File.AppendAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"),
                Environment.NewLine + ex);
            Close();
        }
    }
#if DEBUG
    private async void TestButton_Click(object? sender, RoutedEventArgs e)
    {
        //DownloadLink = @"https://github.com/cybertron-cube/FFmpegAvalonia/releases/download/1.0.2/win-x86.zip";
        string downloadPath = @"A:\CyberPlayerMPV\build\win-x64-multi.zip";
        ExtractDestPath = @"A:\CyberPlayerMPV\build\win-x64-multi";
        AppToLaunchPath = @"A:\CyberPlayerMPV\build\win-x64-multi\CyberVideoPlayer.exe";
        Preservables = new[] { "settings.json", @"updater\CybertronUpdater.exe", @"updater\av_libglesv2.dll", @"updater\libHarfBuzzSharp.dll", @"updater\libSkiaSharp.dll" }.ToList();
        WildcardPreserves = new[] { "*.log" };
        
        
        var dirInfo = new DirectoryInfo(ExtractDestPath);
        if (WildcardPreserves[0] != string.Empty)
        {
            foreach (var searchPattern in WildcardPreserves)
            {
                var addFiles = dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);
                foreach (var addFile in addFiles)
                {
                    Preservables.Add(GenStatic.GetRelativePathFromFull(ExtractDestPath, addFile.FullName));
                }
            }
        }
        
        string debug = DownloadLink + Environment.NewLine + DownloadPath + Environment.NewLine
            + ExtractDestPath + Environment.NewLine + AppToLaunchPath + Environment.NewLine
            + "IGNORABLES" + Environment.NewLine + String.Join(Environment.NewLine, Preservables);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "updater.log"), debug);

        //UILabel.Text = "Downloading update...";
        //await Updater.DownloadUpdatesProgressAsync(DownloadLink, DownloadPath, HttpClient, new Progress<double>(x => UIProgress.Value = x));

        //Remove files that aren't in preservables
        UILabel.Text = "Removing files...";
        var intersect = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => !Preservables.Any(y => y == GenStatic.GetRelativePathFromFull(ExtractDestPath, x.FullName)));
        int total = intersect.Count();
        int current = 0;
        foreach (var file in intersect)
        {
            file.Delete();
            current++;
            UIProgress.Value = (double)current / total;
        }

        //Extract zip file contents to destination
        var updater = new Updater();
        updater.OnNextFile += Updater_OnNextFile;
        await updater.ExtractToDirectoryProgressAsync(
            downloadPath,
            ExtractDestPath,
            Preservables,
            new Progress<double>(x => UIProgress.Value = x));
        UILabel.Text = "Deleting temporary install files...";
        UIProgress.IsVisible = false;
        File.Delete(DownloadPath);
        Process.Start(AppToLaunchPath);
        this.Close();
    }
#endif
    private void Updater_OnNextFile(string filePath)
    {
        var text = $"Extracting {Path.GetFileName(filePath)} to {Path.GetDirectoryName(filePath)}";
        File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"),
            Environment.NewLine + text);
        UILabel.Text = text;
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.IO;
using Cybertron.CUpdater;
using System.Diagnostics;
using System.Reflection;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;

namespace UpdaterAvalonia;

public partial class MainWindow : Window
{
    public HttpClient HttpClient = new();
    public string DownloadPath = Path.GetTempFileName();
    public string DownloadLink;
    public string ExtractDestPath;
    public string AppToLaunchPath;
    public string[] Ignorables;
    public MainWindow()
    {
        InitializeComponent();
#if !DEBUG
        Loaded += MainWindow_Loaded;
#endif
#if DEBUG
        var testButton = new Button();
        UIPanel.Children.Add(testButton);
        testButton.Content = "Test";
        testButton.HorizontalAlignment = HorizontalAlignment.Right;
        testButton.Click += TestButton_Click!;
#endif
    }
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            string debug = DownloadLink + Environment.NewLine + DownloadPath + Environment.NewLine
            + ExtractDestPath + Environment.NewLine + AppToLaunchPath + Environment.NewLine
            + "--IGNORABLES--" + Environment.NewLine + String.Join(Environment.NewLine, Ignorables);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "updater.log"), debug);

            UILabel.Text = "Downloading update...";
            await Updater.DownloadUpdatesProgressAsync(DownloadLink, DownloadPath, HttpClient, new Progress<double>(x => UIProgress.Value = x));

            var updater = new Updater();
            updater.OnNextFile += Updater_OnNextFile;
            await updater.ExtractToDirectoryProgressAsync(
                DownloadPath,
                ExtractDestPath,
                Ignorables,
                new Progress<double>(x => UIProgress.Value = x));
            UILabel.Text = "Deleting temporary install files...";
            UIProgress.IsVisible = false;
            File.Delete(DownloadPath);
            Process.Start(AppToLaunchPath);
            this.Close(); 
        }
        catch (Exception ex)
        {
            File.Delete(DownloadPath);
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "updater.log"), Environment.NewLine + ex.ToString());
            this.Close();
        }
    }
#if DEBUG
    private async void TestButton_Click(object? sender, RoutedEventArgs e)
    {
        DownloadLink = @"https://github.com/Blitznir/FFmpegAvalonia/releases/download/1.0.2/win-x86.zip";
        ExtractDestPath = @"J:\test";
        AppToLaunchPath = "explorer.exe";
        Ignorables = new[] { "profiles.xml", "settings.xml" };

        string debug = DownloadLink + Environment.NewLine + DownloadPath + Environment.NewLine
            + ExtractDestPath + Environment.NewLine + AppToLaunchPath + Environment.NewLine
            + "IGNORABLES" + Environment.NewLine + String.Join(Environment.NewLine, Ignorables);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "updater.log"), debug);

        UILabel.Text = "Downloading update...";
        await Updater.DownloadUpdatesProgressAsync(DownloadLink, DownloadPath, HttpClient, new Progress<double>(x => UIProgress.Value = x));

        var updater = new Updater();
        updater.OnNextFile += Updater_OnNextFile;
        await updater.ExtractToDirectoryProgressAsync(
            DownloadPath,
            ExtractDestPath,
            Ignorables,
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
        string text = $"Extracting {Path.GetFileName(filePath)} to {Path.GetDirectoryName(filePath)}";
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "updater.log"), Environment.NewLine + text);
        UILabel.Text = text;
    }
}
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
using System.Threading.Tasks;
using Cybertron;

namespace UpdaterAvalonia;

public partial class MainWindow : Window
{
    public readonly HttpClient HttpClient = new();
    public readonly string DownloadPath = Path.GetTempFileName();
    public UpdaterArgs UpdaterArgs;

    private List<string> _allIgnore;
    
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
            // Check to see if the process is closed yet, otherwise wait for it to close
            var processes = Process.GetProcessesByName(UpdaterArgs.ProcName);
            InvalidOperationException? procEx = null;
            if (processes.Length > 0)
            {
                var taskList = processes
                    .Where(proc => proc.MainModule?.FileName == UpdaterArgs.AppToLaunch)
                    .Select(proc => proc.WaitForExitAsync());
                
                UILabel.Text = "Waiting on application to close...";
                try
                {
                    await Task.WhenAll(taskList);
                }
                catch (InvalidOperationException exc)
                {
                    procEx = exc;
                }
            }
            
            var dirInfo = new DirectoryInfo(UpdaterArgs.ExtractDestination);
            _allIgnore = new List<string>(UpdaterArgs.Preservables);
            if (UpdaterArgs.WildCardPreserve.Any())
            {
                foreach (var searchPattern in UpdaterArgs.WildCardPreserve)
                {
                    var addFiles = dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);
                    foreach (var addFile in addFiles)
                    {
                        _allIgnore.Add(GenStatic.GetRelativePathFromFull(UpdaterArgs.ExtractDestination, addFile.FullName));
                    }
                }
            }
            
            var debug = UpdaterArgs + Environment.NewLine + "--IGNORABLES--" + Environment.NewLine
                        + string.Join(Environment.NewLine, _allIgnore) + Environment.NewLine + procEx
                        + Environment.NewLine;
            
            await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"), debug);
            
            // Download the update (archive file)
            UILabel.Text = "Downloading update...";
            await Updater.DownloadUpdatesProgressAsync(UpdaterArgs.DownloadLink, DownloadPath, HttpClient, 
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));
            
            // Remove files that aren't in preservables
            UILabel.Text = "Removing files...";
            var intersect = dirInfo
                .EnumerateFiles("*", searchOption: SearchOption.AllDirectories)
                .ExceptBy(_allIgnore, x => x.Name)
                .ToArray();
            var total = intersect.Length;
            for (int i = 0; i < total; i++)
            {
                var file = intersect[i];
                
                if (file.Directory?.Name != "updater")
                    file.Delete();
                
                UIProgress.Value = (double)i / total;
            }
            
            // Remove empty directories
            var rmDirs = dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories)
                .Where(x => !x.EnumerateFiles("*", SearchOption.AllDirectories).Any());
            foreach (var dir in rmDirs)
                if (dir.Exists)
                    dir.Delete();
            
            // Extract archive file contents to destination
            var updater = new Updater();
            updater.OnNextFile += Updater_OnNextFile;
            await updater.ExtractToDirectoryProgressAsync(
                DownloadPath,
                UpdaterArgs.ExtractDestination,
                _allIgnore,
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));

            // Remove archive file, start process, then exit
            UILabel.Text = "Deleting temporary install files...";
            UIProgress.IsVisible = false;
            File.Delete(DownloadPath);
            
            // This executable will be run with admin privileges (on windows)
            // We don't want to give admin privileges to the app
            // If we do drag and drop will not work if used on windows
            if (!OperatingSystem.IsWindows())
                Process.Start(UpdaterArgs.AppToLaunch);
            
            Close();
        }
        catch (Exception ex)
        {
            File.Delete(DownloadPath);
            
            var debug = UpdaterArgs + Environment.NewLine + "--IGNORABLES--" + Environment.NewLine
                        + string.Join(Environment.NewLine, _allIgnore) + Environment.NewLine + ex
                        + Environment.NewLine;
            
            await File.AppendAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"), 
                debug);
            Close();
        }
    }
#if DEBUG
    private async void TestButton_Click(object? sender, RoutedEventArgs e)
    {
        //
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
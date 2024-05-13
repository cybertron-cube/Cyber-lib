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
            if (UpdaterArgs.WildCardPreserve != string.Empty)
            {
                var wildcardPreserves = UpdaterArgs.WildCardPreserve.Split(';');
                foreach (var searchPattern in wildcardPreserves)
                {
                    var addFiles = dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);
                    foreach (var addFile in addFiles)
                    {
                        UpdaterArgs.Preservables.Add(GenStatic.GetRelativePathFromFull(UpdaterArgs.ExtractDestination, addFile.FullName));
                    }
                }
            }
            
            var debug = UpdaterArgs.DownloadLink + Environment.NewLine + DownloadPath + Environment.NewLine 
                        + UpdaterArgs.ExtractDestination + Environment.NewLine + UpdaterArgs.AppToLaunch + Environment.NewLine
                        + "--IGNORABLES--" + Environment.NewLine + string.Join(Environment.NewLine, UpdaterArgs.Preservables)
                         + Environment.NewLine + procEx;
            
            await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.log"), debug);
            
            // Download the update (archive file)
            UILabel.Text = "Downloading update...";
            await Updater.DownloadUpdatesProgressAsync(UpdaterArgs.DownloadLink, DownloadPath, HttpClient, 
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));
            
            // Remove files that aren't in preservables
            UILabel.Text = "Removing files...";
            var intersect = dirInfo.EnumerateFiles().ExceptBy(UpdaterArgs.Preservables, x => x.Name);
            var total = intersect.Count();
            var current = 0;
            foreach (var file in intersect)
            {
                file.Delete();
                current++;
                UIProgress.Value = (double)current / total;
            }

            // Extract archive file contents to destination
            var updater = new Updater();
            updater.OnNextFile += Updater_OnNextFile;
            await updater.ExtractToDirectoryProgressAsync(
                DownloadPath,
                UpdaterArgs.ExtractDestination,
                UpdaterArgs.Preservables,
                new ThreadSafeProgress<double>(x => UIProgress.Value = x));

            // Remove archive file, start process, then exit
            UILabel.Text = "Deleting temporary install files...";
            UIProgress.IsVisible = false;
            File.Delete(DownloadPath);
            
            Process.Start(UpdaterArgs.AppToLaunch);
            
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Helpers;

public static class FileHelper
{
    public static List<EntryFile?> Data = [];
    private static EntryFile? _selected;
    public static EntryFile? Selected
    {
        get => _selected;
        set
        {
            if (!ReferenceEquals(_selected, value))
            {
                _selected = value;
                Refresh?.Invoke(null, EventArgs.Empty);
            }
        }
    }
    public static event EventHandler? Refresh;

    private static FileSystemWatcher? _watcher;
    private static readonly ConcurrentDictionary<string, Timer> DebounceTimers = new(StringComparer.OrdinalIgnoreCase);

    static FileHelper() => StartWatching();

    public static void StartWatching()
    {
        var folder = GetLogFolder();
        Data = LoadAllFiles(); 
        Selected = Data.FirstOrDefault();

        _watcher = new FileSystemWatcher(folder)
        {
            NotifyFilter = NotifyFilters.FileName
                           | NotifyFilters.LastWrite
                           | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnRawFileChanged;
        _watcher.Created += OnRawFileChanged;
        _watcher.Deleted += OnRawFileChanged;
        _watcher.Renamed += OnRawFileChanged;
    }

    private static void OnRawFileChanged(object sender, FileSystemEventArgs e)
    {
        var timer = DebounceTimers.GetOrAdd(e.FullPath, _ => new Timer(
            _ => OnDebouncedChange(e.FullPath), null, Timeout.Infinite, Timeout.Infinite));

        timer.Change(300, Timeout.Infinite);
    }

    private static void OnDebouncedChange(string path)
    {
        if (DebounceTimers.TryRemove(path, out var timer))
        {
            timer.Dispose();
        }

        Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                Data = LoadAllFiles();
                Selected = Data.FirstOrDefault();
                Refresh?.Invoke(null, EventArgs.Empty);
            }));
    }

    private static List<EntryFile?> LoadAllFiles()
    {
        var folder = GetLogFolder();
        var files = Directory.GetFiles(folder);

        return files.Select(f => LoadFileWithRetries(f, 10, 50)).ToList();
    }

    private static EntryFile? LoadFileWithRetries(
        string filePath, int maxRetries, int baseDelayMs)
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var sr = new StreamReader(fs);
                var json = sr.ReadToEnd();

                var data = JsonConvert.DeserializeObject<List<LogEntry>>(json,
                    new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        MaxDepth = 500
                    }) ?? [];

                var fi = new FileInfo(filePath);
                return new EntryFile
                {
                    FileName = fi.Name,
                    Updated = fi.LastWriteTimeUtc,
                    Data = data
                };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(baseDelayMs * (attempt + 1));
            }
        }

        MessageBox.Show($"Failed to load log: {Path.GetFileName(filePath)}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return null;
    }

    private static string GetLogFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MethodLogger");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Helpers;

public static class FileHelper
{
    public static List<EntryFile> Data = LoadLogData() ?? [];
    public static EntryFile Selected = Data.FirstOrDefault();
    public static Func<List<EntryFile>> RefreshData = () => Data = LoadLogData();

    private static string GetLogFolder()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return folder;
    }

    public static List<EntryFile> LoadLogData()
    {
        var folderPath = GetLogFolder();
        string[] files = Directory.GetFiles(folderPath);

           return files.Select(LoadFile).ToList();
    }

    private static EntryFile LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show("Log file not found at: " + filePath);
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<List<LogEntry>>(json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
            });
            var fileInfo = new FileInfo(filePath);
            return new EntryFile
            {
                Updated = fileInfo.LastWriteTimeUtc,
                FileName = fileInfo.Name,
                Data = data
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading log data: " + ex.Message);
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer;

public static class FileHelper
{
    public static List<LogEntry> Data = LoadLogData();
    public static Func<List<LogEntry>> RefreshData = ()=> Data = LoadLogData();

    private static string GetLogFilePath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, "loggeroutput.json");
    }

    public static List<LogEntry> LoadLogData()
    {
        var filePath = GetLogFilePath();
        if (!File.Exists(filePath))
        {
            MessageBox.Show("Log file not found at: " + filePath);
            return [];
        }
        try
        {
            var json =File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<List<LogEntry>>(json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
            });
            return data;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading log data: " + ex.Message);
            return [];

        }
    }
}
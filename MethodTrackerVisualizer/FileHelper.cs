using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using StepByStepLogger;

namespace MethodTrackerVisualizer;

public static class FileHelper
{
    public static string FilePath = GetLogFilePath();
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
        if (!File.Exists(FilePath))
        {
            MessageBox.Show("Log file not found at: " + FilePath);
            return [];
        }
        try
        {
            var json =File.ReadAllText(FilePath);
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
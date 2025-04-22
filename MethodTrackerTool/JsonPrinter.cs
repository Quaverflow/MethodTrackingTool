using MethodTrackerTool.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using MethodTrackerTool.Models;

namespace MethodTrackerTool;

public static class JsonPrinter
{
    public static void WriteLogFile(List<LogEntry> entries, string testName)
    {
        var timestamp = DateTime.Now.ToString("_yyyyMMdd_HHmmss");
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        var tempFolder = Path.Combine(folder, "Temp");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }     
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var combinedName = $"{testName}_{timestamp}.json";
        var tempPath = Path.Combine(tempFolder, combinedName);
        using var fs = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        SerializerHelpers.StreamSerialize(entries, fs);
        fs.Flush(true);
        fs.Close();

        var path = Path.Combine(folder, combinedName);
        File.Move(tempPath, path);
    }
}
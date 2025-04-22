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
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        var path = Path.Combine(folder, $"{testName}_{timestamp}.json");
        using var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        SerializerHelpers.StreamSerialize(entries, fs);
        fs.Flush(true);
    }
}
using MethodTrackerTool.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using MethodTrackerTool.Models;

namespace MethodTrackerTool;

public static class JsonPrinter
{
    public static void WriteLogFile(List<LogEntry> output, string testName)
    {
        var timestamp = DateTime.Now.ToString("_yyyyMMdd_HHmmss");
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        using var fs = File.Create(Path.Combine(folder, $"{testName}_{timestamp}.json"));
        SerializerHelpers.StreamSerialize(output, fs);
    }
}
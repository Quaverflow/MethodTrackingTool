using System;
using System.IO;

namespace MethodTrackerTool;

public static class JsonPrinter
{
    public static void WriteLogFile(string output, string testName)
    {
        var timestamp = DateTime.Now.ToString("_yyyyMMdd_HHmmss");

        var path = CreateFile(testName + timestamp);
        File.WriteAllText(path, output);
    }

    private static string CreateFile(string testName)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, $"{testName}.json");
    }
}
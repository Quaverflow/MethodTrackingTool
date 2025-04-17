using System;
using System.Linq;
using MethodTrackerTool.Helpers;
using Newtonsoft.Json;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming

public static class MethodLogger
{
    private static bool _isInitialized;

    public static void Initialize(string name)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("You can only run this in one test");
        }
        _isInitialized = true;
        MethodPatches.Result = new(name);
        HarmonyInitializer.PatchAssemblies();
    }

    public static void PrintJson()
    {
        var data = MethodPatches.Result;
        var output = JsonConvert.SerializeObject(data.TopLevelCalls, SerializerHelpers.SerializerSettings);
        JsonPrinter.WriteLogFile(output, data.Name);
        if (data.UnexpectedIssues.Any())
        {
            throw new UnexpectedMethodTrackerException();
        }
    }
}
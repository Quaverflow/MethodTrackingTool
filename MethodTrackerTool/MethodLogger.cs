using System;
using System.Linq;
using System.Threading.Tasks;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;
using Newtonsoft.Json;

namespace MethodTrackerTool;
// ReSharper disable InconsistentNaming
public class MethodLogger
{
    static MethodLogger() => HarmonyInitializer.PatchAssemblies();
    private readonly string _name;

    private MethodLogger(string name)
    {
        _name = name;
        ClientPatcher.Initialize();
        MethodPatches.Initialize(name);
    }

    public static async Task InitializeAsync(string name, Func<Task> run)
    {
        var logger = new MethodLogger(name);
        await logger.InitializeAsync(run);
    }

    public static void Initialize(string name, Action run)
    {
        var logger = new MethodLogger(name);
        logger.Initialize(run);
    }

    private async Task InitializeAsync(Func<Task> run)
    {
        await run();
        PrintJson();
    }

    private void Initialize(Action run)
    {
        run();
        PrintJson();
    }

    private void PrintJson()
    {
        MethodPatches.DisableLogging();
        var data = MethodPatches.ResultsByTest[_name];
        JsonPrinter.WriteLogFile(data.TopLevelCalls, data.Name);
        if (data.UnexpectedIssues.Any())
        {
            throw new UnexpectedMethodTrackerException(_name);
        }
    }
}
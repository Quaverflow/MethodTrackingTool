using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Public;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming
public static class MethodLogger
{
    public static readonly List<MethodInfo> Tests = [];
    private static readonly Harmony _harmonyInstance = new("com.method.logger");
    private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    static MethodLogger() => Startup();

    private static void Startup()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        Patch(assemblies);
        foreach (var method in assemblies.SelectMany(x => x.GetTypes().SelectMany(y => y.GetMethods(_bindingFlags))))
        {
            if (method.GetCustomAttribute<TestToWatchAttribute>() != null)
            {
                Tests.Add(method);
            }
        }
    }

    private static void Patch(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var attribute = assembly.GetCustomAttribute<AssemblyToPatchAttribute>();
            if (attribute != null)
            {
                PatchAssembly(assembly);
            }
        }
    }

    private static void PatchAssembly(Assembly targetAssembly)
    {
        var methods = targetAssembly.GetTypes()
            .Where(type => !MethodLoggerHelpers.IsSystemType(type) && !MethodLoggerHelpers.IsTestType(type))
            .SelectMany(type => type.GetMethods(_bindingFlags))
            .Where(MethodLoggerHelpers.IsValidMethod);

        foreach (var method in methods)
        {
            var postfixMethodName = method.ReturnType == typeof(void)
                ? nameof(Patches.LogVoidMethodExit)
                : nameof(Patches.LogMethodExit);

            var prefix = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.LogMethodEntry), _bindingFlags));
            var postfix = new HarmonyMethod(typeof(Patches).GetMethod(postfixMethodName, _bindingFlags));
            var finalizer = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.Finalizer), _bindingFlags));
            try
            {
                _harmonyInstance?.Patch(method, prefix: prefix, postfix: postfix, finalizer: finalizer);
            }
            catch
            {
                // ignore unpatched methods
            }
        }
    }

    private static string PrintJson()
    {
        var output = JsonSerializer.Serialize(Patches.TopLevelCalls, SerializerHelpers.SerializerOptions);
        WriteLogFile(output);

        return Patches.UnexpectedIssues.Any() ? throw new UnexpectedMethodTrackerException() : output;
    }

    private static string GetLogFilePath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, "loggeroutput.json");
    }

    private static void WriteLogFile(string content)
    {
        var path = GetLogFilePath();
        File.WriteAllText(path, content);
    }
}
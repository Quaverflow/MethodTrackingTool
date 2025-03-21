using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Public;
using Newtonsoft.Json;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming

public static class MethodLogger
{
    private static readonly Harmony _harmonyInstance = new("com.method.logger");
    private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static bool _isInitialized;

    public static void Initialize(string name)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("You can only run this in one test");
        }
        _isInitialized = true;
        var data = MethodPatches.Result = new(name);
        PatchAssemblies();
    }

    public static void PrintJson()
    {
        var data = MethodPatches.Result;
        var output = JsonConvert.SerializeObject(data.TopLevelCalls, SerializerHelpers.SerializerSettings);
        WriteLogFile(output, data.Name);
        if (data.UnexpectedIssues.Any())
        {
            throw new UnexpectedMethodTrackerException();
        }
    }

    public static IEnumerable<Assembly> FindAssemblyMarkers() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes(typeof(AssemblyMarkerAttribute), inherit: false)
                .Cast<AssemblyMarkerAttribute>()
                .Select(x => x.Assembly)
                .ToArray());

    private static void PatchAssemblies()
    {
        var assemblies = FindAssemblyMarkers();
        foreach (var assembly in assemblies)
        {
            PatchAssemblyMethods(assembly);
        }
    }
 
    private static void PatchAssemblyMethods(Assembly targetAssembly)
    {
        var methods = targetAssembly.GetTypes()
            .Where(type => !MethodLoggerHelpers.IsSystemType(type) && !MethodLoggerHelpers.IsTestType(type))
            .SelectMany(type => type.GetMethods(_bindingFlags))
            .Where(MethodLoggerHelpers.IsValidMethod);

        foreach (var method in methods)
        {
            var postfixMethodName = method.ReturnType == typeof(void)
                ? nameof(MethodPatches.LogVoidMethodExit)
                : nameof(MethodPatches.LogMethodExit);

            var prefix = new HarmonyMethod(typeof(MethodPatches).GetMethod(nameof(MethodPatches.LogMethodEntry), _bindingFlags));
            var postfix = new HarmonyMethod(typeof(MethodPatches).GetMethod(postfixMethodName, _bindingFlags));
            var finalizer = new HarmonyMethod(typeof(MethodPatches).GetMethod(nameof(MethodPatches.Finalizer), _bindingFlags));
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

    private static void WriteLogFile(string output, string testName)
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
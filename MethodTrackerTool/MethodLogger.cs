using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming

public static class MethodLogger
{
    private static readonly Harmony _harmonyInstance = new("com.method.logger");
    private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static bool _isInitialized;

    public static void Initialize(params Assembly[] assemblies)
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;
        PatchTests();
        PatchAssemblies(assemblies);
        MethodPatches.AllTestsCompleted += PrintJson;
    }


    private static void PatchAssemblies(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            PatchAssemblyMethods(assembly);
        }
    }

    private static void PatchTests()
    {
        foreach (var test in TestPatches.Tests)
        {

            try
            {
                var prefix = new HarmonyMethod(typeof(MethodPatches).GetMethod(nameof(TestPatches.Prefix), _bindingFlags));

                _harmonyInstance?.Patch(test, prefix: prefix);
            }
            catch
            {
                // ignore unpatched methods
            }
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

    internal static void PrintJson()
    {
        var results = MethodPatches.Tests;
        foreach (var result in results)
        {
            var output = JsonSerializer.Serialize(result.Value, SerializerHelpers.SerializerOptions);
            WriteLogFile(output, result.Value.Name);
            if (result.Value.UnexpectedIssues.Any())
            {
                throw new UnexpectedMethodTrackerException();
            }
        }
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

    private static void WriteLogFile(string output, string testName)
    {

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        var path = CreateFile(testName + timestamp);
        File.WriteAllText(testName, output);
    }
}
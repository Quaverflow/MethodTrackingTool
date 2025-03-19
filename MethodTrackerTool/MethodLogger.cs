using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming
public static class MethodLogger
{
    private static Harmony? _harmonyInstance;

    private static Action<string> _loggerOutput = _ => { };
    private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static bool _enabled;

    /// <summary>
    /// Enables logging for the assembly of the specified types
    /// </summary>
    public static void EnableLogging(Action<string> outputAction, params Type[] targetType)
    {
        if (_enabled)
        {
            throw new InvalidOperationException("The patch is applied globally on the first run of this method. Trying to do so again might yield unexpected behaviours.");
        }
        _enabled = true;
        EnableLogging(outputAction, targetType.Select(x => x.Assembly).ToArray());
    }

    /// <summary>
    /// Enables logging for all valid methods in the specified assemblies.
    /// </summary>
    public static void EnableLogging(Action<string> outputAction, params Assembly[] targetAssemblies)
    {
        if (_harmonyInstance == null)
        {
            _harmonyInstance = new Harmony("com.stepbystep.logger");
            _loggerOutput = outputAction;
        }

        foreach (var asm in targetAssemblies)
        {
            PatchAssembly(asm);
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

    public static string Log()
    {
        var output = JsonSerializer.Serialize(Patches.TopLevelCalls, SerializerHelpers.SerializerOptions);
        _loggerOutput(output);

        if (Patches.UnexpectedIssues.Any())
        {
            throw new UnexpectedMethodTrackerException();
        }
        return output;
    }

    public static string PrintJson()
    {
        var output = JsonSerializer.Serialize(Patches.TopLevelCalls, SerializerHelpers.SerializerOptions);
        WriteLogFile(output);

        if (Patches.UnexpectedIssues.Any())
        {
            throw new UnexpectedMethodTrackerException();
        }
        return output;
    }

    public static string GetLogFilePath()
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
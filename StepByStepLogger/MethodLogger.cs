using System.Reflection;
using HarmonyLib;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace StepByStepLogger;

// ReSharper disable InconsistentNaming

public static class MethodLogger
{
    private static Harmony? _harmonyInstance;
    private static readonly List<MethodInfo> PatchedMethods = new();
    private static readonly List<LogEntry> TopLevelCalls = new();
    private static readonly Stack<LogEntry> CallStack = new();
    private static Action<string> _loggerOutput = _ => { };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 200,
    };

    public static MethodLoggerOptions Options { get; } = new();

    /// <summary>
    /// Enables logging for the assembly of the specified types
    /// </summary>
    public static void EnableLogging(Action<string> outputAction, params Type[] targetType)
    {
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
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(MethodLoggerHelpers.IsValidMethod);

        foreach (var method in methods)
        {
            var postfixMethodName = method.ReturnType == typeof(void)
                ? nameof(LogVoidMethodExit)
                : nameof(LogMethodExit);

            var prefix = new HarmonyMethod(typeof(MethodLogger).GetMethod(nameof(LogMethodEntry),
                BindingFlags.Static | BindingFlags.NonPublic));
            var postfix = new HarmonyMethod(typeof(MethodLogger).GetMethod(postfixMethodName,
                BindingFlags.Static | BindingFlags.NonPublic));

            try
            {
                var patchResult = _harmonyInstance?.Patch(method, prefix: prefix, postfix: postfix);
                if (patchResult != null)
                {
                    PatchedMethods.Add(method);
                }
            }
            catch
            {
                // ignore unpatched methods
            }
        }
    }

    public static string PrintJson()
    {
        var output = JsonSerializer.Serialize(TopLevelCalls, SerializerOptions);
        _loggerOutput(output);
        return output;
    }

    /// <summary>
    /// Unpatches all methods and outputs the final call log.
    /// </summary>
    public static string DisableLogging()
    {
        if (_harmonyInstance == null)
        {
            return string.Empty;
        }

        foreach (var method in PatchedMethods)
        {
            _harmonyInstance.Unpatch(method, HarmonyPatchType.All);
        }

        _harmonyInstance = null;
        PatchedMethods.Clear();

        var result = PrintJson();

        TopLevelCalls.Clear();
        CallStack.Clear();

        return result;
    }

    private static void LogMethodEntry(MethodBase __originalMethod, object?[]? __args)
    {
        var argsText = __args != null
            ? __args.Select(arg => arg?.ToString() ?? "null").ToList()
            : new List<string>();
        var entry = new LogEntry
        {
            MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
            Parameters = argsText,
            RawStartTime = DateTime.UtcNow
        };
        if (Options.IncludePerformanceMetrics)
        {
            entry.StartTime = entry.RawStartTime.ToString(Options.DateTimeFormat);
        }

        CallStack.Push(entry);
        Options.RaiseLogEntry(entry);
    }

    private static void LogVoidMethodExit(MethodBase __originalMethod)
    {
        if (CallStack.Count > 0)
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnValue = "void";
        }
    }

    private static void LogMethodExit(MethodBase __originalMethod, object? __result)
    {
        if (CallStack.Count > 0)
        {
            var entry = CommonLogMethodExitSetup();
            if (__result == null)
            {
                entry.ReturnValueType = "null";
                return;
            }

            entry.ReturnValueType = __result.GetType().Name;
            try
            {
                entry.ReturnValue = JsonSerializer.Serialize(__result, SerializerOptions);
            }
            catch (Exception e)
            {
                _loggerOutput(__result.GetType().FullName ?? "");
            }
        }
    }

    private static LogEntry CommonLogMethodExitSetup()
    {
        var entry = CallStack.Pop();
        entry.RawEndTime = DateTime.UtcNow;
        if (Options.IncludePerformanceMetrics)
        {
            entry.EndTime = entry.RawEndTime.ToString(Options.DateTimeFormat);
            entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
            entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
        }

        if (CallStack.Count > 0)
        {
            CallStack.Peek().Children.Add(entry);
        }
        else
        {
            TopLevelCalls.Add(entry);
        }

        return entry;
    }
}
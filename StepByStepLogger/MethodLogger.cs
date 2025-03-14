using System.Reflection;
using HarmonyLib;
using System.Text.Json;
// ReSharper disable InconsistentNaming

namespace StepByStepLogger;
public static class MethodLogger
{
    private static Harmony? _harmonyInstance;
    private static readonly List<MethodInfo> PatchedMethods = new();
    private static readonly List<LogEntry> TopLevelCalls = new();
    private static readonly Stack<LogEntry> CallStack = new();
    private static Action<string> _loggerOutput = _ => { };
    public static MethodLoggerOptions Options { get; } = new();

    /// <summary>
    /// Enables logging for all valid methods in the specified assembly.
    /// If a Harmony instance already exists, it will be used to patch the new assembly.
    /// </summary>
    public static void EnableLogging(Assembly targetAssembly, Action<string> outputAction)
    {
        if (_harmonyInstance == null)
        {
            _harmonyInstance = new Harmony("com.stepbystep.logger");
            _loggerOutput = outputAction;
        }

        PatchAssembly(targetAssembly);
    }

    /// <summary>
    /// Enables logging for all valid methods in the specified assemblies.
    /// </summary>
    public static void EnableLogging(IEnumerable<Assembly> targetAssemblies, Action<string> outputAction)
    {
        foreach (var asm in targetAssemblies)
        {
            EnableLogging(asm, outputAction);
        }
    }

    private static void PatchAssembly(Assembly targetAssembly)
    {
        foreach (var type in targetAssembly.GetTypes())
        {
            if (IsSystemType(type) || IsTestType(type))
                continue;

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                   BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!IsValidMethod(method))
                    continue;

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
                        _loggerOutput($"✅ Patched: {method.DeclaringType?.Name}.{method.Name}");
                        PatchedMethods.Add(method);
                    }
                    else
                    {
                        _loggerOutput($"⚠️ Failed to patch: {method.DeclaringType?.Name}.{method.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _loggerOutput($"❌ Exception patching {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Unpatches all methods and outputs the final call log.
    /// </summary>
    public static void DisableLogging()
    {
        if (_harmonyInstance == null)
            return;

        foreach (var method in PatchedMethods)
        {
            _harmonyInstance.Unpatch(method, HarmonyPatchType.All);
        }

        _harmonyInstance = null;
        PatchedMethods.Clear();

        // Decide whether to include performance metrics based on Options.
        var output = Options.IncludePerformanceMetrics
            ? JsonSerializer.Serialize(TopLevelCalls, new JsonSerializerOptions { WriteIndented = true })
            : JsonSerializer.Serialize(TopLevelCalls.Select(ToMinimal),
                new JsonSerializerOptions { WriteIndented = true });

        _loggerOutput(output);

        TopLevelCalls.Clear();
        CallStack.Clear();
    }

    private static MinimalLogEntry ToMinimal(LogEntry entry)
    {
        return new MinimalLogEntry
        {
            MethodName = entry.MethodName,
            Parameters = entry.Parameters,
            ReturnValue = entry.ReturnValue,
            ReturnValueType = entry.ReturnValueType,
            Children = entry.Children.Select(child => ToMinimal(child)).ToList()
        };
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
            var entry = CallStack.Pop();
            entry.RawEndTime = DateTime.UtcNow;
            if (Options.IncludePerformanceMetrics)
            {
                entry.EndTime = entry.RawEndTime.ToString(Options.DateTimeFormat);
                entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
                entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
            }

            entry.ReturnValue = "void";
            if (CallStack.Count > 0)
                CallStack.Peek().Children.Add(entry);
            else
                TopLevelCalls.Add(entry);
        }
    }

    private static void LogMethodExit(MethodBase __originalMethod, object? __result)
    {
        if (CallStack.Count > 0)
        {
            var entry = CallStack.Pop();
            entry.RawEndTime = DateTime.UtcNow;
            if (Options.IncludePerformanceMetrics)
            {
                entry.EndTime = entry.RawEndTime.ToString(Options.DateTimeFormat);
                entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
                entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
            }

            entry.ReturnValue = __result;
            if (CallStack.Count > 0)
                CallStack.Peek().Children.Add(entry);
            else
                TopLevelCalls.Add(entry);
        }
    }

    private static bool IsValidMethod(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsAbstract || method.DeclaringType == null)
            return false;
        if (method.DeclaringType.Namespace?.StartsWith("System") == true ||
            method.DeclaringType.Namespace?.StartsWith("Microsoft") == true)
            return false;
        if (method.Name.StartsWith("<"))
            return false;
        return !method.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Fact") ||
                                                         attr.GetType().Name.Contains("Test"));
    }

    private static bool IsSystemType(Type type)
    {
        return type.Namespace?.StartsWith("System") == true ||
               type.Namespace?.StartsWith("Microsoft") == true;
    }

    private static bool IsTestType(Type type)
    {
        return type.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Test") ||
                                                      attr.GetType().Name.Contains("Fact"));
    }
}
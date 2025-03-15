using System.Reflection;
using HarmonyLib;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace StepByStepLogger;

// ReSharper disable InconsistentNaming
public class LogEntryConverter : JsonConverter<LogEntry>
{
    public override LogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialization is not supported.
        throw new NotImplementedException("Deserialization is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, LogEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(nameof(LogEntry.MethodName), value.MethodName);

        writer.WritePropertyName(nameof(LogEntry.Parameters));
        JsonSerializer.Serialize(writer, value.Parameters, options);

        writer.WritePropertyName(nameof(LogEntry.ReturnValue));
        JsonSerializer.Serialize(writer, value.ReturnValue, options);

        writer.WriteString(nameof(LogEntry.ReturnType), value.ReturnType);

        if (MethodLogger.Options.IncludePerformanceMetrics)
        {
            writer.WriteString(nameof(LogEntry.StartTime), value.StartTime);
            writer.WriteString(nameof(LogEntry.EndTime), value.EndTime);
            writer.WriteString(nameof(LogEntry.ElapsedTime), value.ElapsedTime);
            writer.WriteString(nameof(LogEntry.ExclusiveElapsedTime), value.ExclusiveElapsedTime);
        }

        writer.WritePropertyName(nameof(LogEntry.Children));
        JsonSerializer.Serialize(writer, value.Children, options);

        writer.WriteEndObject();
    }
}

public static class MethodLogger
{
    private static Harmony? _harmonyInstance;
    private static readonly List<MethodInfo> PatchedMethods = [];
    private static readonly List<LogEntry> TopLevelCalls = [];
    private static readonly Stack<LogEntry> CallStack = new();
    private static Action<string> _loggerOutput = _ => { };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
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
        WriteLogFile(output);
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

    private static void LogMethodEntry(MethodInfo __originalMethod, object?[]? __args)
    {
        var parameters = __originalMethod.GetParameters();
        var argsText = __args != null
            ? __args.Select((arg, i) => new{Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}", Value = arg  })
                .ToDictionary(x => x.Key, x=> x.Value)
            : [];

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

    private static void LogVoidMethodExit(MethodInfo __originalMethod)
    {
        if (CallStack.Count > 0)
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = "N/A";
            AddToStack(entry);
        }
    }

    private static void LogMethodExit(MethodInfo __originalMethod, object? __result)
    {
        if (__result is Task task)
        {
            task.ContinueWith(t =>
            {
                var entry = CommonLogMethodExitSetup();
                var taskResult = task.GetType().GenericTypeArguments.Any() ? GetTaskResult(t) : t;

                entry.ReturnType = BuildReturnTypeString(__originalMethod);
                entry.ReturnValue = SerializeReturnValue(taskResult);
                AddToStack(entry);
            });
        }
        else
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = SerializeReturnValue(__result);
            AddToStack(entry);

        }
    }

    private static string BuildReturnTypeString(MethodInfo __originalMethod)
    {
        return __originalMethod.ReturnType.Namespace + "." + __originalMethod.ReturnType.Name;
    }

    private static object SerializeReturnValue(object? result)
    {
        if (result == null)
        {
            return "null";
        }

        if (result is Type)
        {
            return "System.Type is not supported by the serializer";
        }

        try
        {
            JsonSerializer.Serialize(result, SerializerOptions);
            return result;
        }
        catch (Exception)
        {
            return $"Unserializable type: {result.GetType().FullName}";
        }
    }

    private static void AddToStack(LogEntry entry)
    {
        if (CallStack.Count > 0)
        {
            CallStack.Peek().Children.Add(entry);
        }
        else
        {
            TopLevelCalls.Add(entry);
        }
    }

    private static object? GetTaskResult(Task task)
    {
        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        catch
        {
            return null;
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

        return entry;
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

    public static void WriteLogFile(string content)
    {
        string path = GetLogFilePath();
        File.WriteAllText(path, content);
    }
}
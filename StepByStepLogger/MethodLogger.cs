using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;

namespace MethodTrackerTool;

// ReSharper disable InconsistentNaming


public static class MethodLogger
{
    private static Harmony? _harmonyInstance;

    private static Action<string> _loggerOutput = _ => { };
    private static readonly BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

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
            .SelectMany(type => type.GetMethods(_bindingFlags))
            .Where(MethodLoggerHelpers.IsValidMethod);

        foreach (var method in methods)
        {
            var postfixMethodName = method.ReturnType == typeof(void)
                ? nameof(Patches.LogVoidMethodExit)
                : nameof(Patches.LogMethodExit);

            var prefix = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.LogMethodEntry), _bindingFlags));
            var postfix = new HarmonyMethod(typeof(Patches).GetMethod(postfixMethodName, _bindingFlags));

            try
            {
                _harmonyInstance?.Patch(method, prefix: prefix, postfix: postfix);
            }
            catch
            {
                // ignore unpatched methods
            }
        }
    }

    public static string PrintJson()
    {
        var output = JsonSerializer.Serialize(Patches.TopLevelCalls, SerializerHelpers.SerializerOptions);
        _loggerOutput(output);
        WriteLogFile(output);
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

    public static void WriteLogFile(string content)
    {
        var path = GetLogFilePath();
        File.WriteAllText(path, content);
    }
}

public static class Patches
{
    internal static readonly List<LogEntry> TopLevelCalls = [];
    internal static readonly Stack<LogEntry> CallStack = new();

    internal static void LogMethodEntry(MethodInfo __originalMethod, object?[]? __args)
    {
        var parameters = __originalMethod.GetParameters();
        var argsDictionary =
            __args?.Select((arg, i) => new
            {
                Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}",
                Value = ConvertToSerializableValue(arg)
            })
                .ToDictionary(x => x.Key, x => x.Value);


        var entry = new LogEntry
        {
            MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
            Parameters = argsDictionary ?? [],
            RawStartTime = DateTime.UtcNow
        };

        CallStack.Push(entry);
    }

    public static void LogVoidMethodExit(MethodInfo __originalMethod)
    {
        if (CallStack.Count > 0)
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = "N/A";
            AddToStack(entry);
        }
    }

    internal static void LogMethodExit(MethodInfo __originalMethod, object? __result)
    {
        if (__result is Task task)
        {
            task.ContinueWith(t =>
            {
                var entry = CommonLogMethodExitSetup();
                var taskResult = task.GetType().GenericTypeArguments.Any() ? GetTaskResult(t) : t;

                entry.ReturnType = BuildReturnTypeString(__originalMethod);
                entry.ReturnValue = ConvertToSerializableValue(taskResult);
                AddToStack(entry);
            });
        }
        else
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = ConvertToSerializableValue(__result);
            AddToStack(entry);

        }
    }

    private static string BuildTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var baseName = type.Name;
        var backTickIndex = baseName.IndexOf('`');
        if (backTickIndex > 0)
        {
            baseName = baseName[..backTickIndex];
        }

        var genericArgs = type.GetGenericArguments();
        var genericArgsString = string.Join(", ", genericArgs.Select(BuildTypeName));

        var ns = type.Namespace != null ? type.Namespace + "." : "";
        return $"{ns}{baseName}<{genericArgsString}>";
    }

    private static object ConvertToSerializableValue(object? result)
    {
        if (result is null)
        {
            return "null";
        }

        if (result is Type)
        {
            return "System.Type is not supported by the serializer";
        }

        if (result is Delegate)
        {
            return "System.Delegate is not supported by the serializer";
        }

        try
        {
            JsonSerializer.Serialize(result, SerializerHelpers.SerializerOptions);
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

        entry.EndTime = entry.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
        entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
        entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
        return entry;
    }

    private static string BuildReturnTypeString(MethodInfo method)
    {
        var returnType = method.ReturnType;
        return BuildTypeName(returnType);
    }

}
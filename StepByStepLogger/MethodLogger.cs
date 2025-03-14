using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StepByStepLogger
{
    // Full log entry with performance metrics.
    public class LogEntry
    {
        // Raw timing values (not serialized)
        [JsonIgnore]
        public DateTime RawStartTime { get; set; }
        [JsonIgnore]
        public DateTime RawEndTime { get; set; }
        [JsonIgnore]
        public double RawElapsedMilliseconds => (RawEndTime - RawStartTime).TotalMilliseconds;
        [JsonIgnore]
        public double RawExclusiveElapsedMilliseconds => RawElapsedMilliseconds - Children.Sum(child => child.RawElapsedMilliseconds);

        // Core properties.
        public string MethodName { get; set; } = "";
        public List<string> Parameters { get; set; } = new();
        public object? ReturnValue { get; set; }
        public string ReturnValueType =>
            ReturnValue == null ? "null" :
            ReturnValue is string s && s == "void" ? "void" :
            ReturnValue.GetType().Name;

        // Formatted timing values.
        public string StartTime => RawStartTime.ToString("HH:mm:ss:ff d/M/yyyy");
        public string EndTime => RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
        public string ElapsedTime => $"{RawElapsedMilliseconds:F3} ms";
        public string ExclusiveElapsedTime => $"{RawExclusiveElapsedMilliseconds:F3} ms";

        // Children appear at the bottom.
        public List<LogEntry> Children { get; set; } = new();
    }

    // Minimal output log entry that excludes performance metrics.
    public class OutputLogEntry
    {
        public string MethodName { get; set; } = "";
        public List<string> Parameters { get; set; } = new();
        public object? ReturnValue { get; set; }
        public string ReturnValueType { get; set; } = "";
        public List<OutputLogEntry> Children { get; set; } = new();
    }

    public static class MethodLogger
    {
        private static Harmony? _harmonyInstance;
        private static readonly List<MethodInfo> PatchedMethods = new();
        private static readonly List<LogEntry> TopLevelCalls = new();
        private static readonly Stack<LogEntry> CallStack = new();
        private static Action<string> _loggerOutput = _ => { };

        // Controls whether performance metrics are included in the output.
        public static bool IncludePerformanceMetrics { get; set; } = true;

        public static void EnableLogging(Assembly targetAssembly, Action<string> outputAction)
        {
            if (_harmonyInstance != null)
                return;

            _harmonyInstance = new Harmony("com.stepbystep.logger");
            _loggerOutput = outputAction;

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

                    var prefix = new HarmonyMethod(typeof(MethodLogger).GetMethod(nameof(LogMethodEntry), BindingFlags.Static | BindingFlags.NonPublic));
                    var postfix = new HarmonyMethod(typeof(MethodLogger).GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.NonPublic));

                    try
                    {
                        var patchResult = _harmonyInstance.Patch(method, prefix: prefix, postfix: postfix);
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

            // If performance metrics are not desired, transform to minimal output.
            string json;
            if (IncludePerformanceMetrics)
            {
                json = JsonSerializer.Serialize(TopLevelCalls, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var minimalOutput = TopLevelCalls.Select(entry => ToMinimal(entry)).ToList();
                json = JsonSerializer.Serialize(minimalOutput, new JsonSerializerOptions { WriteIndented = true });
            }
            _loggerOutput(json);

            TopLevelCalls.Clear();
            CallStack.Clear();
        }

        // Converts a full LogEntry to an OutputLogEntry (without timing properties).
        private static OutputLogEntry ToMinimal(LogEntry entry)
        {
            return new OutputLogEntry
            {
                MethodName = entry.MethodName,
                Parameters = entry.Parameters,
                ReturnValue = entry.ReturnValue,
                ReturnValueType = entry.ReturnValueType,
                Children = entry.Children.Select(child => ToMinimal(child)).ToList()
            };
        }

        // Harmony prefix hook: called before the original method.
        private static void LogMethodEntry(MethodBase __originalMethod, object?[]? __args)
        {
            var argsText = __args != null ? __args.Select(arg => arg?.ToString() ?? "null").ToList() : new List<string>();
            var entry = new LogEntry
            {
                MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
                Parameters = argsText,
                RawStartTime = DateTime.UtcNow
            };
            CallStack.Push(entry);
        }

        // Harmony postfix hook for void methods.
        private static void LogVoidMethodExit(MethodBase __originalMethod)
        {
            if (CallStack.Count > 0)
            {
                var entry = CallStack.Pop();
                entry.RawEndTime = DateTime.UtcNow;
                entry.ReturnValue = "void";
                if (CallStack.Count > 0)
                    CallStack.Peek().Children.Add(entry);
                else
                    TopLevelCalls.Add(entry);
            }
        }

        // Harmony postfix hook for methods with a return value.
        private static void LogMethodExit(MethodBase __originalMethod, object? __result)
        {
            if (CallStack.Count > 0)
            {
                var entry = CallStack.Pop();
                entry.RawEndTime = DateTime.UtcNow;
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
}

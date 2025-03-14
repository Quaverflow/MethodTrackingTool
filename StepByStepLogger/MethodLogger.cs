using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.Text.Json;

namespace StepByStepLogger
{
    // Represents a logged method call.
    public class LogEntry
    {
        // Raw timing values (only used if performance metrics are enabled).
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime RawStartTime { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime RawEndTime { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public double RawElapsedMilliseconds => (RawEndTime - RawStartTime).TotalMilliseconds;
        [System.Text.Json.Serialization.JsonIgnore]
        public double RawExclusiveElapsedMilliseconds => RawElapsedMilliseconds - Children.Sum(child => child.RawElapsedMilliseconds);

        // Core properties.
        public string MethodName { get; set; } = "";
        public List<string> Parameters { get; set; } = new();
        public object? ReturnValue { get; set; }
        public string ReturnValueType =>
            ReturnValue == null ? "null" :
            ReturnValue is string s && s == "void" ? "void" :
            ReturnValue.GetType().Name;

        // Formatted timing values (if performance metrics are enabled).
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? ElapsedTime { get; set; }
        public string? ExclusiveElapsedTime { get; set; }

        // Children appear at the bottom.
        public List<LogEntry> Children { get; set; } = new();
    }

    // Minimal output version (if performance metrics are disabled) remains available from previous versions,
    // but for brevity we'll use LogEntry as our output model.

    public static class MethodLogger
    {
        private static Harmony? _harmonyInstance;
        private static readonly List<MethodInfo> PatchedMethods = new();
        private static readonly List<LogEntry> TopLevelCalls = new();
        private static readonly Stack<LogEntry> CallStack = new();
        private static Action<string> _loggerOutput = _ => { };

        // Configuration options remain in the Options property.
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
            // Otherwise, reuse the existing _harmonyInstance and output action.
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
            string output = Options.IncludePerformanceMetrics
                ? JsonSerializer.Serialize(TopLevelCalls, new JsonSerializerOptions { WriteIndented = true })
                : JsonSerializer.Serialize(TopLevelCalls.Select(entry => ToMinimal(entry)), new JsonSerializerOptions { WriteIndented = true });

            _loggerOutput(output);

            TopLevelCalls.Clear();
            CallStack.Clear();
        }

        // Converts a full LogEntry to a minimal version (without timing data).
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
            if (Options.IncludePerformanceMetrics)
            {
                entry.StartTime = entry.RawStartTime.ToString(Options.DateTimeFormat);
            }
            CallStack.Push(entry);
            Options.RaiseLogEntry(entry);
        }

        // Harmony postfix hook for void methods.
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

        // Harmony postfix hook for methods with a return value.
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

    // Minimal output log entry (without timing data)
    public class OutputLogEntry
    {
        public string MethodName { get; set; } = "";
        public List<string> Parameters { get; set; } = new();
        public object? ReturnValue { get; set; }
        public string ReturnValueType { get; set; } = "";
        public List<OutputLogEntry> Children { get; set; } = new();
    }

    // Configuration options for the logger.
    public class MethodLoggerOptions
    {
        /// <summary>
        /// If true, performance metrics (timing data) are included.
        /// </summary>
        public bool IncludePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Format string for dates/times (used if performance metrics are enabled).
        /// </summary>
        public string DateTimeFormat { get; set; } = "HH:mm:ss:ff d/M/yyyy";

        /// <summary>
        /// Optional custom output formatter. If provided, this function is used to convert the call tree to a string.
        /// </summary>
        public Func<List<LogEntry>, string>? OutputFormatter { get; set; } = null;

        /// <summary>
        /// If true, real-time logging events will be fired as log entries are created.
        /// </summary>
        public bool EnableRealTimeLogging { get; set; } = false;

        /// <summary>
        /// Event fired when a new log entry is created (if EnableRealTimeLogging is true).
        /// </summary>
        public event Action<LogEntry>? OnLogEntry;

        internal void RaiseLogEntry(LogEntry entry)
        {
            if (EnableRealTimeLogging)
                OnLogEntry?.Invoke(entry);
        }
    }

    public class MethodLoggerFixture : IDisposable
    {
        /// <summary>
        /// Creates a new fixture that patches the provided assemblies.
        /// </summary>
        /// <param name="targetAssemblies">The assemblies to patch.</param>
        /// <param name="outputAction">The action to output log messages (e.g. Console.WriteLine or ITestOutputHelper.WriteLine).</param>
        /// <param name="options">Configuration options for the logger.</param>
        public MethodLoggerFixture(IEnumerable<Assembly> targetAssemblies, Action<string> outputAction, MethodLoggerOptions options)
        {
            // Configure the logger using the provided options.
            MethodLogger.Options.IncludePerformanceMetrics = options.IncludePerformanceMetrics;
            MethodLogger.Options.DateTimeFormat = options.DateTimeFormat;
            MethodLogger.Options.OutputFormatter = options.OutputFormatter;
            MethodLogger.Options.EnableRealTimeLogging = options.EnableRealTimeLogging;
            if (options.EnableRealTimeLogging)
            {
                options.OnLogEntry += entry => outputAction($"RealTime: {entry.MethodName}");
            }

            // Enable logging for all provided assemblies.
            MethodLogger.EnableLogging(targetAssemblies, outputAction);
        }

        public void Dispose()
        {
            MethodLogger.DisableLogging();
        }
    }

}

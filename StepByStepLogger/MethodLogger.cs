using System.Reflection;
using HarmonyLib;

namespace StepByStepLogger;

/// <summary>
/// Provides functionality to patch methods in a given assembly using Harmony
/// and log their invocations. The call log is stored internally and can be flushed later.
/// </summary>
public static class MethodLogger
{
    private static Harmony? _harmonyInstance;
    private static readonly List<MethodInfo> PatchedMethods = new();
    private static readonly List<string> CallTree = new(); // Stores all logged call messages

    private static Action<string> _loggerOutput = _ => { };

    /// <summary>
    /// Applies Harmony patches to all valid methods in the specified assembly.
    /// </summary>
    /// <param name="targetAssembly">The assembly whose methods will be patched.</param>
    /// <param name="outputAction">
    /// An action to output log messages (for example, passing <c>ITestOutputHelper.WriteLine</c>
    /// or <c>_loggerOutput</c>).
    /// </param>
    public static void EnableLogging(Assembly targetAssembly, Action<string> outputAction)
    {
        if (_harmonyInstance != null)
        {
            return;
        }

        _harmonyInstance = new Harmony("com.stepbystep.logger");
        _loggerOutput = outputAction;

        foreach (var type in targetAssembly.GetTypes())
        {
            if (IsSystemType(type) || IsTestType(type))
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                   BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!IsValidMethod(method))
                {
                    continue;
                }

                // Select appropriate postfix based on return type.
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
                        _loggerOutput($"✅ Successfully patched: {method.DeclaringType?.Name}.{method.Name}");
                        PatchedMethods.Add(method);
                    }
                    else
                    {
                        _loggerOutput($"⚠️ Failed to patch: {method.DeclaringType?.Name}.{method.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _loggerOutput($"❌ Exception while patching {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Removes all patches and flushes the stored call log using the configured output action.
    /// </summary>
    public static void DisableLogging()
    {
        if (_harmonyInstance == null)
        {
            return;
        }

        foreach (var method in PatchedMethods)
        {
            _harmonyInstance.Unpatch(method, HarmonyPatchType.All);
        }

        _harmonyInstance = null;
        PatchedMethods.Clear();

        // Output all logged calls
        foreach (var log in CallTree)
        {
            _loggerOutput(log);
        }

        CallTree.Clear();
    }

    /// <summary>
    /// Prefix hook that logs method entry.
    /// </summary>
    private static void LogMethodEntry(MethodBase __originalMethod, object?[]? __args)
    {
        var argsText = string.Join(", ", __args?.Select(arg => arg?.ToString() ?? "null") ?? []);
        CallTree.Add($"📌 Called: {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}({argsText})");
    }

    /// <summary>
    /// Postfix hook for methods that return void.
    /// </summary>
    private static void LogVoidMethodExit(MethodBase __originalMethod)
    {
        CallTree.Add($"📌 Returned: void from {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}");
    }

    /// <summary>
    /// Postfix hook for methods that return a value.
    /// </summary>
    private static void LogMethodExit(MethodBase __originalMethod, object? __result)
    {
        CallTree.Add($"📌 Returned: {__result ?? "null"} from {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}");
    }

    /// <summary>
    /// Determines whether the specified method is valid for patching.
    /// </summary>
    private static bool IsValidMethod(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsAbstract || method.DeclaringType == null)
        {
            return false;
        }

        if (method.DeclaringType.Namespace?.StartsWith("System") == true ||
            method.DeclaringType.Namespace?.StartsWith("Microsoft") == true)
        {
            return false;
        }

        if (method.Name.StartsWith("<"))
        {
            return false;
        }

        // Exclude methods decorated with attributes indicating test methods.
        return !method.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Fact") || attr.GetType().Name.Contains("Test"));
    }

    /// <summary>
    /// Determines whether the specified type is a system type.
    /// </summary>
    private static bool IsSystemType(Type type)
    {
        return type.Namespace?.StartsWith("System") == true || type.Namespace?.StartsWith("Microsoft") == true;
    }

    /// <summary>
    /// Determines whether the specified type is a test type.
    /// </summary>
    private static bool IsTestType(Type type)
    {
        return type.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Test") || attr.GetType().Name.Contains("Fact"));
    }
}